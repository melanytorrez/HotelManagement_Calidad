import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { NuevaReservaService } from '../../../core/services/nueva-reserva.service';
import { Subscription, timer } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HabitacionService } from '../../../core/services/habitacion.service';
import { OrderByNumeroPipe } from './order-by-numero.pipe';

@Component({
  selector: 'app-habitaciones-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, OrderByNumeroPipe],
  templateUrl: './habitaciones-list.component.html',
  styleUrls: ['./habitaciones-list.component.scss']
})
export class HabitacionesListComponent implements OnInit, OnDestroy {
    mensajeExito: string | null = null;
  private readonly api = inject(NuevaReservaService);

  habitaciones: any[] = [];
  loading = true;

  // Lista de tipos para el select
  tipos: any[] = [];

  // Modales
  modalEditarAbierto = false;
  habitacionAEditar: any = null;
  // nuevo: guardar estado original para detectar cambios
  originalEstado: string | null = null;

  modalEliminarAbierto = false;
  habitacionAEliminar: any = null;

  private pollSub: Subscription | null = null;

  // Para búsqueda por número de habitación
  busquedaNumero: string = '';

  constructor(
    private readonly habitacionService: HabitacionService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    // Leer mensaje de éxito desde query param si viene de creación
    const url = new URL(window.location.href);
    const exito = url.searchParams.get('exito');
    if (exito) {
      this.mensajeExito = exito;
      setTimeout(() => this.mensajeExito = null, 3000);
      // Limpiar el query param de la URL sin recargar
      window.history.replaceState({}, document.title, window.location.pathname);
    }

    this.cargarHabitaciones();

    // Cargar tipos de habitación para el select
    this.habitacionService.getTiposHabitacion().subscribe({
      next: (t: any[]) => {
        this.tipos = t || [];
      },
      error: (err: any) => {
        console.error('Error cargando tipos de habitación:', err);
      }
    });

    // Polling cada 15s para reflejar cambios en BD (puedes ajustar intervalo)
    this.pollSub = timer(15000, 15000).subscribe(() => {
      this.cargarHabitaciones();
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  goBack(): void {
    this.router.navigate(['/inicio']);
  }

  cargarHabitaciones() {
    this.loading = true;
    this.habitacionService.getHabitaciones().subscribe({
      next: (data) => {
        this.habitaciones = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error cargando habitaciones:', err);
        this.loading = false;
      }
    });
  }

  // Editar
  abrirModalEditar(h: any) {
    console.log('=== HABITACIÓN ORIGINAL ===');
    console.log('Datos completos de h:', h);
    
    // Intentar obtener el tipoId de diferentes posibles propiedades
    const tipoId = h.tipoId ?? h.tipo_Id ?? h.Tipo_Habitacion_ID ?? h.tipo_Habitacion_ID ?? null;
    
    this.habitacionAEditar = { 
      id: h.id,
      numero: h.numero,
      piso: h.piso,
      tipoId: tipoId,  // Usar el valor que encontramos
      capacidad: h.capacidad,
      estado: h.estado || 'Libre'
    };
    
    console.log('habitacionAEditar creado:', this.habitacionAEditar);
    this.modalEditarAbierto = true;
  }

  cerrarModalEditar() {
    this.modalEditarAbierto = false;
    this.habitacionAEditar = null;
  }

    guardarEdicion() {
    if (!this.habitacionAEditar.id) {
      console.error('Error: No se encontró el ID de la habitación');
      alert('Error: No se encontró el ID de la habitación');
      return;
    }

    if (!this.habitacionAEditar.tipoId) {
      console.error('Error: No se seleccionó un tipo de habitación');
      alert('Error: Debe seleccionar un tipo de habitación');
      return;
    }

    console.log('=== DATOS DE EDICIÓN ===');
    console.log('habitacionAEditar:', this.habitacionAEditar);
    console.log('tipos disponibles:', this.tipos);

    const tipoSeleccionado = this.tipos.find(
      (t) => t.id === this.habitacionAEditar.tipoId
    );
    console.log('Tipo seleccionado:', tipoSeleccionado);

    const tipoNombre =
      tipoSeleccionado?.tipo_Nombre ??
      tipoSeleccionado?.nombre ??
      tipoSeleccionado?.tipoNombre ??
      '';

    const tipoIdFinal = this.habitacionAEditar.tipoId;
    const payload = this.construirPayloadEdicion(tipoNombre, tipoIdFinal);

    console.log('=== PAYLOAD A ENVIAR ===');
    console.log(JSON.stringify(payload, null, 2));

    this.habitacionService.updateHabitacion(payload).subscribe({
      next: (response) => this.manejarActualizacionExitosa(response),
      error: (err: any) => this.manejarErrorActualizacion(err)
    });
  }

  private buscarHabitacionPorNumero(numero: string): any {
    return this.habitaciones.find((h) => h.numero === numero);
  }
  private mostrarMensajeExitoTemporal(mensaje: string): void {
    this.mensajeExito = mensaje;
    setTimeout(() => {
      this.mensajeExito = null;
    }, 3000);
  }
    private verificarCambioHabitacion(
    numero: string,
    estadoEsperado: string,
    intentos: number,
    maxIntentos: number
  ): void {
    this.cargarHabitaciones();
    setTimeout(() => {
      this.evaluarCambioHabitacion(numero, estadoEsperado, intentos, maxIntentos);
    }, 500);
  }
  private evaluarCambioHabitacion(
    numero: string,
    estadoEsperado: string,
    intentos: number,
    maxIntentos: number
  ): void {
    const hab = this.buscarHabitacionPorNumero(numero);

    if (hab && hab.estado === estadoEsperado) {
      this.mostrarMensajeExitoTemporal('Habitación actualizada correctamente');
      this.cerrarModalEditar();
      return;
    }

    if (intentos + 1 < maxIntentos) {
      this.verificarCambioHabitacion(numero, estadoEsperado, intentos + 1, maxIntentos);
      return;
    }

    this.cerrarModalEditar();
    alert('El estado no se reflejó en la tabla tras actualizar.');
  }
    private construirPayloadEdicion(tipoNombre: string, tipoIdFinal: string): any {
    return {
      id: this.habitacionAEditar.id,
      ID: this.habitacionAEditar.id,
      numero_Habitacion: this.habitacionAEditar.numero,
      piso: this.habitacionAEditar.piso,
      tipo_Id: tipoIdFinal,
      Tipo_Habitacion_ID: tipoIdFinal,
      tipo_Nombre: tipoNombre,
      capacidad_Maxima: Number.parseInt(this.habitacionAEditar.capacidad, 10),
      estado_Habitacion: this.habitacionAEditar.estado || 'Libre'
    };
  }

  private manejarActualizacionExitosa(response: any): void {
    console.log('=== RESPUESTA EXITOSA ===');
    console.log('response:', response);

    const numero = this.habitacionAEditar.numero;
    const estadoEsperado = this.habitacionAEditar.estado;

    this.verificarCambioHabitacion(numero, estadoEsperado, 0, 30);
  }
  private manejarErrorActualizacion(err: any): void {
    console.error('=== ERROR AL ACTUALIZAR ===');
    console.error('Error completo:', err);
    console.error('Status:', err.status);
    console.error('Status Text:', err.statusText);
    console.error('Error body:', err.error);
    console.error('URL:', err.url);

    let mensaje = 'No se pudo actualizar la habitación.\n\n';
    if (err.status === 404) {
      mensaje += 'Error 404: La habitación no fue encontrada en el servidor.';
    } else if (err.status === 400) {
      mensaje += 'Error 400: Datos inválidos.\n';
      mensaje += JSON.stringify(err.error);
    } else if (err.status === 500) {
      mensaje += 'Error 500: Error interno del servidor.';
    } else {
      mensaje += `Error ${err.status}: ${err.statusText}`;
    }

    alert(mensaje);
  }


  // Extrae la lógica de actualización completa para reusar desde el fallback
  private _guardarEdicionCompleta(tipoNombre: string, estadoBackend: string) {
    // preparar payload base
    const basePayload = {
      id: this.habitacionAEditar.id,
      ID: this.habitacionAEditar.id,
      numero_Habitacion: this.habitacionAEditar.numero,
      piso: this.habitacionAEditar.piso,
      tipo_Id: this.habitacionAEditar.tipoId ?? null,
      Tipo_Habitacion_ID: this.habitacionAEditar.tipoId ?? null,
      tipo_Nombre: tipoNombre,
      capacidad_Maxima: this.habitacionAEditar.capacidad,
      // estado_Habitacion será asignado dinámicamente en los intentos
    };

    // variantes a probar (genérico, sin rama por 'mantenimiento')
    const estadoBase = estadoBackend;
    const variantesEstado = [estadoBase];
    const up = estadoBase.toUpperCase();
    if (up !== estadoBase) variantesEstado.push(up);
    const estadosUnicos = variantesEstado.filter((v, i, a) => v && a.indexOf(v) === i);

    const tryUpdate = (index: number) => {
      if (index >= estadosUnicos.length) {
        alert('No se pudo guardar la habitación. Intenta nuevamente o revisa el servidor.');
        return;
      }
      const payload = { ...basePayload, estado_Habitacion: estadosUnicos[index] };
      console.log(`Intento ${index + 1}/${estadosUnicos.length} - payload:`, payload);
      this.habitacionService.updateHabitacion(payload).subscribe({
        next: (response) => {
          console.log(`Respuesta HTTP status=${response.status}`, response);
          const actualizado = response.body;
          const updated = actualizado || {
            id: payload.ID,
            numero: payload.numero_Habitacion,
            piso: payload.piso,
            tipoNombre: payload.tipo_Nombre,
            capacidad: payload.capacidad_Maxima,
            estado: payload.estado_Habitacion
          };
          this.habitaciones = this.habitaciones.map(h => h.id === updated.id ? updated : h);
          this.cerrarModalEditar();
        },
        error: (err: any) => {
          console.error(`Error intento ${index + 1}:`, err);
          // Mostrar cuerpo de error si lo devuelve el servidor
          if (err?.error) {
            console.error('Cuerpo de error del servidor:', err.error);
            // Si es 5xx, reintentar con otra variante
            if (this.debeReintentarActualizacion(err?.status, index, estadosUnicos.length)) {
              console.warn('Error 5xx, reintentando con siguiente variante de estado...');
              tryUpdate(index + 1);
              return;
            }
            // Mostrar mensaje detallado al usuario
            const serverMsg = typeof err.error === 'string' ? err.error : JSON.stringify(err.error);
            alert(`No se pudo guardar la habitación. Respuesta servidor: ${serverMsg}`);
            return;
          }

          // si no hay cuerpo, manejo estándar
          if (this.debeReintentarActualizacion(err?.status, index, estadosUnicos.length)) {
            tryUpdate(index + 1);
            return;
          }

          const serverMsg =
            err?.message ||
            (err?.status ? `HTTP ${err.status} ${err.statusText || ''}` : null);
          alert(`No se pudo guardar la habitación. ${serverMsg ? 'Motivo: ' + serverMsg : 'Intenta nuevamente.'}`);
        }
      });
    };

    tryUpdate(0);
  }
  private construirPayloadBaseEdicionCompleta(tipoNombre: string): any {
    return {
      id: this.habitacionAEditar.id,
      ID: this.habitacionAEditar.id,
      numero_Habitacion: this.habitacionAEditar.numero,
      piso: this.habitacionAEditar.piso,
      tipo_Id: this.habitacionAEditar.tipoId ?? null,
      Tipo_Habitacion_ID: this.habitacionAEditar.tipoId ?? null,
      tipo_Nombre: tipoNombre,
      capacidad_Maxima: this.habitacionAEditar.capacidad
    };
  }
  private obtenerEstadosUnicos(estadoBackend: string): string[] {
    const variantesEstado = [estadoBackend];
    const up = estadoBackend.toUpperCase();

    if (up !== estadoBackend) {
      variantesEstado.push(up);
    }

    return variantesEstado.filter((v, i, a) => !!v && a.indexOf(v) === i);
  }
  private debeReintentarActualizacion(
    status: number | undefined,
    index: number,
    totalEstados: number
  ): boolean {
    return status !== undefined && status >= 500 && status < 600 && index < totalEstados - 1;
  }
  private intentarActualizarHabitacionConEstado(
    basePayload: any,
    estadosUnicos: string[],
    index: number
  ): void {
    if (index >= estadosUnicos.length) {
      alert('No se pudo guardar la habitación. Intenta nuevamente o revisa el servidor.');
      return;
    }

    const payload = {
      ...basePayload,
      estado_Habitacion: estadosUnicos[index]
    };

    console.log(`Intento ${index + 1}/${estadosUnicos.length} - payload:`, payload);

    this.habitacionService.updateHabitacion(payload).subscribe({
      next: (response) => {
        console.log(`Respuesta HTTP status=${response.status}`, response);
        const actualizado = response.body;
        const updated = actualizado || {
          id: payload.ID,
          numero: payload.numero_Habitacion,
          piso: payload.piso,
          tipoNombre: payload.tipo_Nombre,
          capacidad: payload.capacidad_Maxima,
          estado: payload.estado_Habitacion
        };

        this.habitaciones = this.habitaciones.map(h => h.id === updated.id ? updated : h);
        this.cerrarModalEditar();
      },
      error: (err: any) => {
        console.error(`Error intento ${index + 1}:`, err);

        if (err?.error) {
          console.error('Cuerpo de error del servidor:', err.error);

          if (this.debeReintentarActualizacion(err?.status, index, estadosUnicos.length)) {
            console.warn('Error 5xx, reintentando con siguiente variante de estado...');
            this.intentarActualizarHabitacionConEstado(basePayload, estadosUnicos, index + 1);
            return;
          }

          const serverMsg =
            typeof err.error === 'string' ? err.error : JSON.stringify(err.error);
          alert(`No se pudo guardar la habitación. Respuesta servidor: ${serverMsg}`);
          return;
        }

        if (this.debeReintentarActualizacion(err?.status, index, estadosUnicos.length)) {
          this.intentarActualizarHabitacionConEstado(basePayload, estadosUnicos, index + 1);
          return;
        }

        const serverMsg =
          err?.message ||
          (err?.status ? `HTTP ${err.status} ${err.statusText || ''}` : null);

        alert(`No se pudo guardar la habitación. ${serverMsg ? 'Motivo: ' + serverMsg : 'Intenta nuevamente.'}`);
      }
    });
  }

  // Eliminar
  abrirModalEliminar(h: any) {
    this.habitacionAEliminar = h;
    this.modalEliminarAbierto = true;
  }

  cerrarModalEliminar() {
    this.modalEliminarAbierto = false;
    this.habitacionAEliminar = null;
  }

  confirmarEliminar() {
    if (!this.habitacionAEliminar) return;
    this.habitacionService.deleteHabitacion(this.habitacionAEliminar.id).subscribe({
      next: () => {
        this.habitaciones = this.habitaciones.filter(h => h.id !== this.habitacionAEliminar.id);
        this.cerrarModalEliminar();
        this.mensajeExito = 'Habitación eliminada correctamente';
        setTimeout(() => this.mensajeExito = null, 3000);
      },
      error: (err: any) => {
        console.error('Error eliminando habitación:', err);
        alert('No se pudo eliminar la habitación. Intenta nuevamente.');
      }
    });
  }

  // Genera la clase CSS para el badge a partir del estado (reemplaza espacios por guiones)
  getStatusClass(status: string | null | undefined): string {
    const s = (status || 'Libre').toString();
    const safe = s.replaceAll(/\s+/g, '-').replaceAll(/[^A-Za-z0-9-]/g, '');
    return 'status-' + safe;
  }

  // Normaliza el estado para lo que espera el backend (convierte a texto con espacios)
  normalizeEstadoForBackend(status: string | null | undefined): string {
    if (!status) return 'Libre';
    const s = status.toString().trim();

    // Mapear variantes comunes (sin 'Mantenimiento')
    if (/^fuera[\s\-_]?de[\s\-_]?servicio$/i.test(s)) return 'Fuera de Servicio';
    if (/^reservad/i.test(s)) return 'Reservada';
    if (/^ocupad/i.test(s)) return 'Ocupada';
    // Por defecto convertir guiones/underscores a espacios
    return s.replaceAll(/[-_]+/g, ' ').trim();
  }

  get habitacionesFiltradas() {
    let filtradas = this.habitaciones;
    if (this.busquedaNumero.trim()) {
      filtradas = filtradas.filter(h =>
        h.numero?.toString().includes(this.busquedaNumero.trim())
      );
    }
    // El pipe orderByNumero se encargará de ordenar en el HTML
    return filtradas;
  }
}