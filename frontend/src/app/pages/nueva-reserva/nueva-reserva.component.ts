import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, FormArray, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';

interface Habitacion {
  id: string;
  numero: string;
  estado: string;
  tarifaBase: number;
}

interface ClienteOption {
  id: string;
  label: string;
  nit: string;
}

interface HuespedOption {
  id: string;
  nombre: string;
  apellido: string;
  segundo_apellido?: string;
}

export interface HabitacionOption {
  id: string;
  numero: string | number;
  tipoNombre?: string;
  piso?: string | number;
  capacidad?: number;
  estado?: string;
  // agrega aquí otros campos que uses en el componente
}
type SearchableValue = string | number | null | undefined;

@Component({
  selector: 'app-nueva-reserva',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './nueva-reserva.component.html',
  styleUrls: ['./nueva-reserva.component.scss']
})
export class NuevaReservaComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  private readonly API_URL = 'http://localhost:5000/api';

  clientes = signal<ClienteOption[]>([]);
  habitaciones = signal<Habitacion[]>([]);
  huespedes = signal<HuespedOption[]>([]);
  catalogosCargando = signal(false);

  submitting = signal(false);
  mensaje = signal<string | null>(null);
  error = signal<string | null>(null);

  pasoActual = signal(1);
  totalPasos = 3;
  habitacionSearchTerm: string[] = [];
  showHabitacionSug: boolean[] = [];

  private readonly norm3 = (v: SearchableValue) =>
  String(v ?? '').toLowerCase().normalize('NFD').replaceAll(/\p{Diacritic}/gu, '');


  estadosReserva = ['Pendiente', 'Confirmada', 'Cancelada'];
  
  filteredHabitaciones(i: number): HabitacionOption[] {
  const term = this.norm3(this.habitacionSearchTerm[i] ?? '');
  const disponibles = this.getHabitacionesLibres(i);
  if (!term) return disponibles;

  return disponibles.filter(h => this.norm3(h.numero).includes(term));
}
private ensureHabitacionStateIndex(i: number) {
  while (this.habitacionSearchTerm.length <= i) this.habitacionSearchTerm.push('');
  while (this.showHabitacionSug.length <= i) this.showHabitacionSug.push(false);
}
openHabitacion(i: number) {
  this.ensureHabitacionStateIndex(i);
  this.showHabitacionSug[i] = true;
}

onHabitacionBlur(i: number) {
  setTimeout(() => this.showHabitacionSug[i] = false, 150);
}

onHabitacionInput(i: number, value: string) {
  this.ensureHabitacionStateIndex(i);
  this.habitacionSearchTerm[i] = value;
}

seleccionarHabitacion(i: number, h: HabitacionOption) {
  this.habitacionesFormArray.at(i).patchValue({ habitacionId: h.id });
  this.habitacionSearchTerm[i] = h.numero.toString();
  this.showHabitacionSug[i] = false;
}

getHabitacionSeleccionada(i: number): string {
  const habitacionId = this.habitacionesFormArray.at(i).get('habitacionId')?.value;
  if (!habitacionId) return '';
  const habitacion = this.habitaciones().find(h => h.id === habitacionId);
  return habitacion ? habitacion.numero.toString() : '';
}

habitacionesLibres = computed<HabitacionOption[]>(() => {
    return (this.habitaciones() ?? []).filter(h => {
      const estado = (h.estado ?? '').toString().toLowerCase();
      return estado === 'libre';
    });
  });
  getHabitacionesLibres(indexActual: number): HabitacionOption[] {
  const todas = this.habitaciones() ?? [];

  // Obtener IDs seleccionados en otros grupos
  const seleccionadas = new Set(
    this.habitacionesFormArray.controls
      .map((fg, i) => i == indexActual ? null : fg.get('habitacionId')?.value)
      .filter(id => !!id)
  );

  return todas.filter(h => {
    const estado = (h.estado ?? '').toString().toLowerCase();
    return estado === 'libre' && !seleccionadas.has(h.id);
  });
}


  private _syncSeleccionConDisponibilidad = effect(() => {
    const libres = this.habitacionesLibres();
    const idsLibres = new Set(libres.map(h => h.id));
    for (const fg of this.habitacionesFormArray.controls) {
      const sel = fg.get('habitacionId')?.value;
      if (sel && !idsLibres.has(sel)) {
        fg.get('habitacionId')?.reset('');
      }
    }
  });

  form = this.fb.nonNullable.group({
    clienteId: ['', Validators.required],
    estadoReserva: ['Pendiente', Validators.required],
    montoTotal: [null as number | null, [Validators.required, Validators.min(0)]],
    habitaciones: this.fb.array<FormGroup>([], Validators.minLength(1))
  });

  paso1Valido = computed(() => {
    const clienteId = this.form.get('clienteId')?.value;
    return !!(clienteId && clienteId !== '');
  });

  paso2Valido = computed(() => {
    const habitaciones = this.habitacionesFormArray;
    return habitaciones.length > 0 && habitaciones.controls.every(h => {
      const huespedIds = h.get('huespedIds')?.value as string[] || [];
      return h.get('habitacionId')?.valid && 
             h.get('fechaEntrada')?.valid && 
             h.get('fechaSalida')?.valid &&
             huespedIds.length > 0;
    });
  });

  paso3Valido = computed(() => {
    return this.form.controls.montoTotal.valid;
  });

  get habitacionesFormArray(): FormArray {
    return this.form.get('habitaciones') as FormArray;
  }

  clienteSeleccionado = computed(() => {
    const clienteId = this.form.controls.clienteId.value;
    const cliente = this.clientes().find(c => c.id === clienteId);
    return cliente?.label || 'No seleccionado';
  });

  showSuggestions = false;
  searchTerm = signal<string>('');
  huespedSearchTerm: string[] = [];
  showHuespedSug: boolean[] = [];

  ngOnInit(): void {
    this.cargarCatalogos();
    this.agregarHabitacion();
  }

  private cargarCatalogos(): void {
    this.catalogosCargando.set(true);
    forkJoin({
      clientes: this.http.get<any[]>(`${this.API_URL}/Cliente`),
      habitaciones: this.http.get<any[]>(`${this.API_URL}/Habitacion`),
      huespedes: this.http.get<any[]>(`${this.API_URL}/Huesped`)
    })
      .pipe(finalize(() => this.catalogosCargando.set(false)))
      .subscribe({
        next: ({ clientes, habitaciones, huespedes }) => {
          this.clientes.set(clientes.map((c: any) => ({
            id: c.id,
            label: c.razon_Social,
            nit: c.nit
          })));
          
          this.habitaciones.set(habitaciones.map((h: any) => ({
            id: h.id,
            numero: h.numero_Habitacion,
            estado: h.estado_Habitacion,
            tarifaBase: h.tarifa_Base || 0
          })));

          this.huespedes.set(huespedes.map((h: any) => ({
            id: h.id,
            nombre: h.nombre || '',
            apellido: h.apellido || h.apellido_Paterno || '',
            segundo_apellido: h.segundo_apellido || h.apellido_Materno || ''
          })));
        },
        error: (err) => {
          console.error('Error al cargar catálogos:', err);
          this.error.set('Error al cargar la información necesaria');
        }
      });
  }

  calcularMontoTotal(): void {
    let montoTotal = 0;

    this.habitacionesFormArray.controls.forEach(habitacionControl => {
      const habitacionId = habitacionControl.get('habitacionId')?.value;
      const fechaEntrada = habitacionControl.get('fechaEntrada')?.value;
      const fechaSalida = habitacionControl.get('fechaSalida')?.value;

      if (habitacionId && fechaEntrada && fechaSalida) {
        const habitacion = this.habitaciones().find(h => h.id === habitacionId);
        
        if (habitacion && habitacion.tarifaBase) {
          const dias = this.calcularDias(fechaEntrada, fechaSalida);
          const subtotal = habitacion.tarifaBase * dias;
          montoTotal += subtotal;
        }
      }
    });

    this.form.patchValue({ montoTotal }, { emitEvent: false });
  }

  private readonly norm = (v: SearchableValue) =>
    String(v ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replaceAll(/\p{Diacritic}/gu, '');

  private readonly digits = (v: SearchableValue) => String(v ?? '').replaceAll(/\D+/g, '');

  filteredClientes = computed<ClienteOption[]>(() => {
    const termRaw = this.searchTerm().trim();
    if (!termRaw) return this.clientes();

    const term = this.norm(termRaw);
    const termDigits = this.digits(termRaw);

    return this.clientes().filter((c: any) => {
      const textParts = [c.label, c.razon_Social, c.email];
      const numParts = [c.nit];

      const textBlob = this.norm(textParts.filter(Boolean).join(' '));
      const digitsBlob = this.digits(numParts.filter(Boolean).join(' '));

      const byText = textBlob.includes(term);
      const byDigits = !!termDigits && digitsBlob.includes(termDigits);

      return byText || byDigits;
    });
  });

  seleccionarCliente(c: any) {
    this.form.controls.clienteId.setValue(c.id);
    this.searchTerm.set(c.label);
    this.showSuggestions = false;
  }

  onBlur() {
    setTimeout(() => this.showSuggestions = false, 200);
  }

  filteredHuespedes(i: number): HuespedOption[] {
    const term = this.norm(this.huespedSearchTerm[i] ?? '');
    const all = this.huespedes();
    if (!term) return all;

    return all.filter(h => this.norm(h.nombre).includes(term));
  }

  openHuesped(i: number) {
    this.ensureHuespedStateIndex(i);
    this.showHuespedSug[i] = true;
  }

  onHuespedBlur(i: number) {
    setTimeout(() => this.showHuespedSug[i] = false, 150);
  }

  onHuespedInput(i: number, value: string) {
    this.ensureHuespedStateIndex(i);
    this.huespedSearchTerm[i] = value;
  }

  seleccionarHuesped(i: number, h: HuespedOption, inputEl?: HTMLInputElement) {
    this.agregarHuesped(i, h.id);
    this.huespedSearchTerm[i] = '';
    if (inputEl) inputEl.value = '';
    this.showHuespedSug[i] = false;
  }

  private ensureHuespedStateIndex(i: number) {
    while (this.huespedSearchTerm.length <= i) this.huespedSearchTerm.push('');
    while (this.showHuespedSug.length <= i) this.showHuespedSug.push(false);
  }

  irAPaso(paso: number): void {
    if (paso >= 1 && paso <= this.totalPasos) {
      this.pasoActual.set(paso);
    }
  }

  siguientePaso(): void {
    if (this.pasoActual() === 1 && this.isPaso1Valid()) {
      this.pasoActual.set(2);
    } else if (this.pasoActual() === 2 && this.isPaso2Valid()) {
      this.pasoActual.set(3);
      this.calcularMontoTotal();
    }
  }

  pasoAnterior(): void {
    if (this.pasoActual() > 1) {
      this.pasoActual.set(this.pasoActual() - 1);
    }
  }

  agregarHabitacion(): void {
    const nuevaHabitacion = this.fb.group({
      habitacionId: ['', Validators.required],
      fechaEntrada: ['', Validators.required],
      fechaSalida: ['', Validators.required],
      huespedIds: [[], Validators.required]
    });

    nuevaHabitacion.valueChanges.subscribe(() => {
      this.calcularMontoTotal();
    });

    this.habitacionesFormArray.push(nuevaHabitacion);
    this.showHuespedSug.push(false);
    this.huespedSearchTerm.push('');
  }

  eliminarHabitacion(index: number): void {
    this.habitacionesFormArray.removeAt(index);
    this.showHuespedSug.splice(index, 1);
    this.huespedSearchTerm.splice(index, 1);
    this.calcularMontoTotal();
  }

  agregarHuesped(habitacionIndex: number, huespedId: string): void {
    const habitacionGroup = this.habitacionesFormArray.at(habitacionIndex);
    const huespedIds = habitacionGroup.get('huespedIds')?.value as string[] || [];
    
    if (huespedId && !huespedIds.includes(huespedId)) {
      habitacionGroup.patchValue({
        huespedIds: [...huespedIds, huespedId]
      });
    }
  }

  eliminarHuesped(habitacionIndex: number, huespedId: string): void {
    const habitacionGroup = this.habitacionesFormArray.at(habitacionIndex);
    const huespedIds = (habitacionGroup.get('huespedIds')?.value as string[] || [])
      .filter(id => id !== huespedId);
    
    habitacionGroup.patchValue({ huespedIds });
  }

  obtenerNombreHuesped(id: string): string {
    const huesped = this.huespedes().find(h => h.id === id);
    if (!huesped) return 'Huésped desconocido';
    
    const partes = [
      huesped.nombre,
      huesped.apellido,
      huesped.segundo_apellido
    ].filter(parte => parte && parte.trim() !== '');
    
    return partes.join(' ') || 'Sin nombre';
  }

  calcularDias(fechaEntrada: string, fechaSalida: string): number {
    if (!fechaEntrada || !fechaSalida) return 0;
    const entrada = new Date(fechaEntrada);
    const salida = new Date(fechaSalida);
    const diferencia = salida.getTime() - entrada.getTime();
    return Math.max(0, Math.ceil(diferencia / (1000 * 60 * 60 * 24)));
  }

  calcularTotalDias(): number {
    let total = 0;
    this.habitacionesFormArray.controls.forEach(h => {
      const entrada = h.get('fechaEntrada')?.value;
      const salida = h.get('fechaSalida')?.value;
      if (entrada && salida) {
        total += this.calcularDias(entrada, salida);
      }
    });
    return total;
  }

  crearReserva(): void {
    if (this.form.invalid || this.submitting()) return;

    this.submitting.set(true);
    this.error.set('');
    this.mensaje.set('');

    const reservaPayload = {
      cliente_ID: this.form.value.clienteId!,
      estado_Reserva: this.form.value.estadoReserva!,
      monto_Total: this.form.value.montoTotal!
    };

    this.http.post<any>('http://localhost:5000/api/Reserva', reservaPayload)
      .pipe(
        switchMap((reservaCreada: any) => {
          const detallesPayload = {
            reserva_ID: reservaCreada.id,
            habitaciones: this.form.value.habitaciones!.map((h: any) => ({
              habitacion_ID: h.habitacionId,
              huesped_IDs: h.huespedIds,
              fecha_Entrada: h.fechaEntrada,
              fecha_Salida: h.fechaSalida
            }))
          };
          return this.http.post('http://localhost:5000/api/DetalleReserva/multiple', detallesPayload);
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.mensaje.set('Reserva creada exitosamente');
          setTimeout(() => this.router.navigate(['/reservas']), 1500);
        },
        error: (err) => {
          console.error('Error al crear reserva:', err);
          this.error.set(err.error?.message || 'Error al crear la reserva');
        }
      });
  }

  volverAlListado(): void {
    this.router.navigate(['/reservas']);
  }

  isPaso1Valid(): boolean {
    const clienteId = this.form.get('clienteId')?.value;
    return !!(clienteId && clienteId !== '');
  }

  isPaso2Valid(): boolean {
    const habitaciones = this.habitacionesFormArray;
    if (habitaciones.length === 0) return false;
    for (const h of habitaciones.controls) {
      const habitacionId = h.get('habitacionId')?.value;
      const fechaEntrada = h.get('fechaEntrada')?.value;
      const fechaSalida = h.get('fechaSalida')?.value;
      const huespedIds = (h.get('huespedIds')?.value as string[]) || [];
      if (!habitacionId || habitacionId === '') return false;
      if (!fechaEntrada || !fechaSalida) return false;
      try {
        if (new Date(fechaSalida) <= new Date(fechaEntrada)) return false;
      } catch {
        return false;
      }
      if (huespedIds.length === 0) return false;
    }
    return true;
  }

  isPaso3Valid(): boolean {
    const montoCtrl = this.form.get('montoTotal');
    return !!montoCtrl && montoCtrl.valid;
  }

  // Mapa para almacenar errores del servidor
  serverErrors = new Map<string, string>();

  // Métodos de validación local
  hasLocalError(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && control.touched);
  }

  getLocalError(field: string): string {
    const control = this.form.get(field);
    if (control?.errors) {
      if (control.errors['required']) return 'Este campo es obligatorio';
      if (control.errors['min']) return 'Valor inválido';
    }
    return '';
  }

  // Métodos de validación de servidor
  hasServerError(field: string): boolean {
    return this.serverErrors.has(field);
  }

  getServerError(field: string): string {
    return this.serverErrors.get(field) || '';
  }
}
