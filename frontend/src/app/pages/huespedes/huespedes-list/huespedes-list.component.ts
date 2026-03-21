import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { HuespedService } from '../../../core/services/huesped.service';
import { HuespedLite, nombreCompleto } from '../../../shared/models/huesped-lite.model';
import { OrderByNombrePipe } from './order-by-nombre.pipe';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-huespedes-list',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, OrderByNombrePipe, FormsModule],
  templateUrl: './huespedes-list.component.html',
  styleUrls: ['./huespedes-list.component.scss']
})
export class HuespedesListComponent implements OnInit {
  huespedes: HuespedLite[] = [];
  loading = true;
  nombreCompleto = nombreCompleto;
  modalEliminarAbierto = false;
  huespedAEliminar: any = null;
  busqueda: string = '';
  modalEditarAbierto = false;
  huespedAEditar: any = null;

  constructor(
    private huespedService: HuespedService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.huespedService.getHuespedes().subscribe({
      next: (hs: HuespedLite[]) => {
        this.huespedes = hs;
        this.loading = false;
      },
      error: () => {
        this.huespedes = [];
        this.loading = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/inicio']);
  }

  eliminarHuesped(id: string): void {
    this.huespedService.deleteHuesped(id).subscribe({
      next: () => {
        this.huespedes = this.huespedes.filter(h => h.id !== id);
      },
      error: (err) => {
        console.error('Error eliminando huésped:', err);
        window.alert('No se pudo eliminar el huésped. Intenta nuevamente.');
      }
    });
  }

  abrirModalEliminar(huesped: any) {
    console.log('abrirModalEliminar llamado', huesped);
    this.huespedAEliminar = huesped;
    this.modalEliminarAbierto = true;
  }

  cerrarModalEliminar() {
    this.modalEliminarAbierto = false;
    this.huespedAEliminar = null;
  }

  confirmarEliminar() {
    if (this.huespedAEliminar) {
      this.eliminarHuesped(this.huespedAEliminar.id);
      this.cerrarModalEliminar();
    }
  }

  abrirModalEditar(huesped: any) {
    // Procesa nombre y segundo nombre
    let primerNombre = huesped.primerNombre || '';
    let segundoNombre = huesped.segundoNombre || '';
    
    // Si primerNombre contiene espacios, separar en primer y segundo nombre
    if (primerNombre.includes(' ') && !segundoNombre) {
      const palabras = primerNombre.split(' ');
      primerNombre = palabras[0]; // Primera palabra es el primer nombre
      segundoNombre = palabras.slice(1).join(' '); // El resto es segundo nombre
    }
    
    // Procesa apellidos
    let primerApellido = huesped.primerApellido || '';
    let segundoApellido = huesped.segundoApellido || '';
    
    // Procesa la fecha
    let fechaNacimiento = huesped.fechaNacimiento || '';
    if (fechaNacimiento) {
      // Convierte a formato yyyy-MM-dd para el input date
      const fecha = new Date(fechaNacimiento);
      if (!Number.isNaN(fecha.getTime())) {
        const yyyy = fecha.getFullYear();
        const mm = String(fecha.getMonth() + 1).padStart(2, '0');
        const dd = String(fecha.getDate()).padStart(2, '0');
        fechaNacimiento = `${yyyy}-${mm}-${dd}`;
      }
    }
    
    this.huespedAEditar = {
      id: huesped.id,
      primerNombre: primerNombre,
      segundoNombre: segundoNombre,
      primerApellido: primerApellido,
      segundoApellido: segundoApellido,
      documento: huesped.documento || '',
      telefono: huesped.telefono || '',
      fechaNacimiento: fechaNacimiento
    };
    
    this.modalEditarAbierto = true;
  }

  cerrarModalEditar() {
    this.modalEditarAbierto = false;
    this.huespedAEditar = null;
  }

  guardarEdicion() {
    if (!this.huespedAEditar) return;
    
    // Log para depuración: ver qué valores tenemos antes de enviar
    console.log('Datos antes de enviar:', {
      primerNombre: this.huespedAEditar.primerNombre,
      segundoNombre: this.huespedAEditar.segundoNombre,
      primerApellido: this.huespedAEditar.primerApellido,
      segundoApellido: this.huespedAEditar.segundoApellido
    });
    
    // Asegura que todos los campos existan y se envíen correctamente
    const huespedActualizado = {
      id: this.huespedAEditar.id,
      primerNombre: this.huespedAEditar.primerNombre || '',
      segundoNombre: this.huespedAEditar.segundoNombre || '', // Aseguramos que se envíe
      primerApellido: this.huespedAEditar.primerApellido || '',
      segundoApellido: this.huespedAEditar.segundoApellido || '',
      documento: this.huespedAEditar.documento || '',
      telefono: this.huespedAEditar.telefono || '',
      fechaNacimiento: this.huespedAEditar.fechaNacimiento || ''
    };
    
    this.huespedService.updateHuesped(huespedActualizado).subscribe({
      next: (actualizado: HuespedLite) => {
        console.log('Huésped actualizado recibido del backend:', actualizado);
        
        // Actualiza con todos los campos correctos, incluyendo segundoNombre
        this.huespedes = this.huespedes.map(h => {
          if (h.id === actualizado.id) {
            // Asegura que se conserve el segundoNombre
            return {
              ...actualizado,
              segundoNombre: actualizado.segundoNombre || huespedActualizado.segundoNombre
            };
          }
          return h;
        });
        
        this.cerrarModalEditar();
      },
      error: (err: any) => {
        console.error('Error al guardar el huésped:', err);
        window.alert('No se pudo guardar los cambios. Intenta nuevamente.');
      }
    });
  }

  // Nuevo: navegar a detalle (ver)
  verHuesped(id: string): void {
    // Asume ruta de detalle: /huespedes/:id
    this.router.navigate(['/huespedes', id]);
  }

  // Nuevo: navegar a editar
  editarHuesped(id: string): void {
    // Asume ruta de edición: /huespedes/editar/:id
    this.router.navigate(['/huespedes/editar', id]);
  }

  get huespedesFiltrados() {
    const filtro = this.busqueda.trim().toLowerCase();
    if (!filtro) return this.huespedes;
    return this.huespedes.filter(h =>
      this.nombreCompleto(h).toLowerCase().includes(filtro) ||
      h.documento?.toLowerCase().includes(filtro)
    );
  }
}
