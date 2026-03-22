import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, switchMap, tap } from 'rxjs';
import { NuevaReservaService, ClienteOption, HabitacionOption, HuespedOption } from '../../core/services/nueva-reserva.service';

@Component({
  selector: 'app-editar-reserva',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './editar-reserva.component.html',
  styleUrls: ['./editar-reserva.component.scss']
})
export class EditarReservaComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(NuevaReservaService);

  clientes = signal<ClienteOption[]>([]);
  habitaciones = signal<HabitacionOption[]>([]);
  huespedes = signal<HuespedOption[]>([]);
  catalogosCargando = signal(false);

  submitting = signal(false);
  mensaje = signal<string | null>(null);
  error = signal<string | null>(null);

  reservaId = '';
  estadosReserva = ['Pendiente', 'Confirmada', 'Cancelada', 'Completada', 'No-Show'];

  form = this.fb.nonNullable.group({
    clienteId: ['', Validators.required],
    habitacionId: ['', Validators.required],
    huespedId: ['', Validators.required],
    estadoReserva: ['', Validators.required],
    fechaEntrada: ['', Validators.required],
    fechaSalida: ['', Validators.required]
  });

  ngOnInit(): void {
    this.reservaId = this.route.snapshot.queryParamMap.get('id') ?? '';
    if (!this.reservaId) {
      this.error.set('No se proporcionó ID de reserva.');
      return;
    }
    this.cargarCatalogos();
  }

  private cargarCatalogos(): void {
    this.catalogosCargando.set(true);
    this.error.set(null);

    this.api.getClientes()
      .pipe(
        tap(list => this.clientes.set(list)),
        switchMap(() => this.api.getHabitaciones()),
        tap(list => this.habitaciones.set(list)),
        switchMap(() => this.api.getHuespedes()),
        tap(list => this.huespedes.set(list)),
        switchMap(() => this.api.getReservaById(this.reservaId)),
        finalize(() => this.catalogosCargando.set(false))
      )
      .subscribe({
        next: (reserva) => {
          this.form.patchValue({
            clienteId: reserva.cliente_ID || '',
            estadoReserva: reserva.estado_Reserva || '',
            fechaEntrada: reserva.fecha_Entrada?.split('T')[0] || '',
            fechaSalida: reserva.fecha_Salida?.split('T')[0] || ''
          });
          this.cargarDetalleReserva();
        },
        error: () => this.error.set('No se pudieron cargar los datos.')
      });
  }

  private cargarDetalleReserva(): void {
    this.api.getDetallesByReservaId(this.reservaId).subscribe({
      next: (detalles) => {
        if (detalles.length > 0) {
          const detalle = detalles[0];
          this.form.patchValue({
            habitacionId: detalle.habitacionId || '',
            huespedId: detalle.huespedId || ''
          });
        }
      },
      error: () => console.error('No se pudo cargar el detalle de la reserva.')
    });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.mensaje.set(null);
    this.submitting.set(true);

    const data = this.form.getRawValue();
    const payloadReserva = {
      cliente_ID: data.clienteId,
      estado_Reserva: data.estadoReserva,
      fecha_Entrada: data.fechaEntrada,
      fecha_Salida: data.fechaSalida
    };

    this.api.updateReserva(this.reservaId, payloadReserva)
      .pipe(
        switchMap(() => this.api.getDetallesByReservaId(this.reservaId)),
        switchMap((detalles) => {
          if (detalles.length === 0) throw new Error('No hay detalles para actualizar.');
          const detalleId = detalles[0].id;
          return this.api.updateDetalleReserva(detalleId, {
            habitacion_ID: data.habitacionId,
            huesped_ID: data.huespedId
          });
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe({
        next: () => {
          this.mensaje.set('Reserva actualizada correctamente.');
          setTimeout(() => this.router.navigate(['/reservas']), 1500);
        },
        error: () => {
          this.error.set('No se pudo actualizar la reserva. Intenta nuevamente.');
        }
      });
  }

  cancelar(): void {
    this.router.navigate(['/reservas']);
  }
}
