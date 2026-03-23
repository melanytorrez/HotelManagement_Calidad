import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';

interface TipoHabitacion {
  id: string;
  nombre: string;
  descripcion: string;
  capacidad_Maxima: number;
  precio_Base: number;
}

@Component({
  selector: 'app-nueva-habitacion',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './nueva-habitacion.component.html',
  styleUrls: ['./nueva-habitacion.component.scss']
})
export class NuevaHabitacionComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  form!: FormGroup;
  tiposHabitacion = signal<TipoHabitacion[]>([]);
  submitting = signal(false);
  mensaje = signal('');
  error = signal('');
  serverErrors = new Map<string, string>();

  estadosDisponibles = ['Disponible', 'Ocupada', 'Mantenimiento', 'Reservada'];

  ngOnInit() {
    this.inicializarFormulario();
    this.cargarTiposHabitacion();
  }

  private inicializarFormulario() {
    this.form = this.fb.group({
      numero: ['', [
        Validators.required,
        this.numeroHabitacionValidator.bind(this)
      ]],
      piso: ['', [Validators.required, Validators.min(1)]],
      tipoHabitacionId: ['', Validators.required],
      estado: ['Disponible']
    });

    // Subscribirse a cambios del campo número para debugging
    this.form.get('numero')?.valueChanges.subscribe(value => {
      console.log('Valor del número:', value);
      console.log('Errores del campo número:', this.form.get('numero')?.errors);
      console.log('Estado del campo:', {
        valid: this.form.get('numero')?.valid,
        invalid: this.form.get('numero')?.invalid,
        dirty: this.form.get('numero')?.dirty,
        touched: this.form.get('numero')?.touched
      });
    });
  }

  // Validador personalizado para número de habitación
  private numeroHabitacionValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null; // Si está vacío, lo maneja 'required'
    }
    
    const valor = control.value.toString().trim();
    
    console.log('Validando número de habitación:', valor);
    
    // Regex: de 1 a 3 dígitos, seguido opcionalmente de UNA letra mayúscula (A-Z)
    // ^[0-9]{1,3} = de 1 a 3 dígitos
    // [A-Z]? = opcionalmente UNA letra mayúscula
    // $ = fin de cadena (no acepta más caracteres)
    const regex = /^[0-9]{1,3}[A-Z]?$/;
    
    const esValido = regex.test(valor);
    
    console.log('¿Es válido?', esValido);
    
    if (!esValido) {
      console.log('Retornando error formatoInvalido');
      return { formatoInvalido: true };
    }
    
    return null;
  }

  // Métodos de validación local
  hasLocalError(field: string): boolean {
    const control = this.form.get(field);
    const hasError = !!(control && control.invalid && control.touched);
    console.log(`hasLocalError(${field}):`, hasError);
    return hasError;
  }

  hasLocalErrorDirty(field: string): boolean {
    const control = this.form.get(field);
    const hasError = !!(control && control.invalid && control.dirty);
    console.log(`hasLocalErrorDirty(${field}):`, hasError);
    return hasError;
  }

  getLocalError(field: string): string {
    const control = this.form.get(field);
    if (control?.errors) {
      console.log(`Errores en ${field}:`, control.errors);
      if (control.errors['required']) return 'Este campo es obligatorio';
      if (control.errors['min']) return 'El piso debe ser mayor a 0';
      if (control.errors['formatoInvalido']) {
        return 'Formato inválido. Use de 1 a 3 números y opcionalmente UNA letra (A-Z). Ejemplos: 1, 12A, 205B';
      }
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

  cargarTiposHabitacion() {
    this.http.get<TipoHabitacion[]>('http://localhost:5000/api/TipoHabitacion')
      .subscribe({
        next: (tipos) => this.tiposHabitacion.set(tipos),
        error: (err) => {
          console.error('Error al cargar tipos de habitación:', err);
          this.error.set('No se pudieron cargar los tipos de habitación');
        }
      });
  }

  guardar() {
    console.log('=== INTENTANDO GUARDAR ===');
    
    // Marcar todos los campos como touched y dirty
    Object.keys(this.form.controls).forEach(key => {
      const control = this.form.get(key);
      control?.markAsTouched();
      control?.markAsDirty();
      control?.updateValueAndValidity();
    });

    console.log('Estado del formulario:', {
      valid: this.form.valid,
      invalid: this.form.invalid,
      value: this.form.value,
      errors: this.form.errors
    });

    console.log('Errores por campo:', {
      numero: this.form.get('numero')?.errors,
      piso: this.form.get('piso')?.errors,
      tipoHabitacionId: this.form.get('tipoHabitacionId')?.errors,
      estado: this.form.get('estado')?.errors
    });

    // Verificar que el formulario sea válido antes de enviar
    if (this.form.invalid) {
      this.error.set('Por favor, corrija los errores en el formulario antes de continuar');
      console.log('❌ FORMULARIO INVÁLIDO - NO SE ENVIARÁ');
      return;
    }

    console.log('✅ Formulario válido, enviando...');

    this.submitting.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.serverErrors.clear();

    const payload = {
      numero_Habitacion: this.form.value.numero!,
      piso: this.form.value.piso!,
      tipo_Habitacion_ID: this.form.value.tipoHabitacionId!,
      estado_Habitacion: this.form.value.estado!
    };

    this.http.post('http://localhost:5000/api/Habitacion', payload)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          // Redirigir pasando mensaje de éxito por query param
          this.router.navigate(['/habitaciones'], { queryParams: { exito: 'Habitación creada correctamente' } });
        },
        error: (err) => {
          console.error('Error al crear habitación:', err);
          this.error.set(err.error?.message || 'Error al crear la habitación');
        }
      });
  }

  cancelar() {
    this.router.navigate(['/habitaciones']);
  }
}
