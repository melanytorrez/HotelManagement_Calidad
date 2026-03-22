import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClienteService } from '../../core/services/cliente.service';

@Component({
  selector: 'app-nuevo-cliente',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './nuevo-cliente.component.html',
  styleUrls: ['./nuevo-cliente.component.scss']
})
export class NuevoClienteComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private api = inject(ClienteService);

  // Signals
  submitting = signal(false);
  mensaje = signal<string | null>(null);
  error = signal<string | null>(null);

  // Mapa para almacenar errores del servidor
  serverErrors = new Map<string, string>();

  // Declarar el formulario sin inicializar (se inicializa en ngOnInit)
  form!: FormGroup;

  ngOnInit() {
    this.inicializarFormulario();
  }

  private inicializarFormulario() {
    this.form = this.fb.group({
      razonSocial: ['', [
        Validators.required,
        Validators.maxLength(20),
        this.razonSocialValidator.bind(this)
      ]],
      nit: ['', [
        Validators.required,
        Validators.minLength(7),
        Validators.maxLength(13),
      ]],
      email: ['', [
        Validators.required,
        Validators.email
      ]]
    });
  }

  // Validador para Razón Social: solo letras, números y espacios
  private razonSocialValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }

    const valor = control.value.toString().trim();
    
    // Solo permite letras (mayúsculas/minúsculas), números, espacios, puntos y comas
    const regex = /^[A-Za-z0-9\s.,]+$/;
    
    if (!regex.test(valor)) {
      return { formatoInvalido: true };
    }

    return null;
  }

  // Validador para NIT: solo números, mínimo 7 dígitos
  private nitValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }

    const valor = control.value.toString().trim();
    
    // Solo números
    const regex = /^\d+$/;
    
    if (!regex.test(valor)) {
      return { formatoInvalido: true };
    }

    // Mínimo 7 dígitos (NIT en Bolivia)
    if (valor.length < 7) {
      return { minimoDigitos: true };
    }

    // Máximo 13 dígitos
    if (valor.length > 13) {
      return { maximoDigitos: true };
    }

    return null;
  }

  guardar(): void {
    // Marcar todos los campos como touched y dirty
    Object.keys(this.form.controls).forEach(key => {
      const control = this.form.get(key);
      control?.markAsTouched();
      control?.markAsDirty();
      control?.updateValueAndValidity();
    });

    // Verificar que el formulario sea válido
    if (this.form.invalid) {
      this.error.set('Por favor, corrija los errores en el formulario antes de continuar');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.mensaje.set(null);
    this.serverErrors.clear();

    // Payload con las claves que espera ClienteCreatePayload
    const payload = {
      razon_Social: (this.form.value.razonSocial || '').toUpperCase(),
      nit: (this.form.value.nit || '').toUpperCase(),
      email: this.form.value.email || ''
    };

    this.api.createCliente(payload).subscribe({
      next: () => {
        this.submitting.set(false);
        this.mensaje.set('Cliente creado correctamente. Redirigiendo...');

        setTimeout(() => {
          this.router.navigate(['/clientes']);
        }, 1500);
      },
      error: (err: any) => {
        this.submitting.set(false);
        console.error('Error creando cliente:', err);
        this.error.set('No se pudo crear el cliente. Intenta nuevamente.');
      }
    });
  }

  cancelar(): void {
    this.router.navigate(['/clientes']);
  }

  // Métodos de validación local
  hasLocalError(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && control.touched);
  }

  hasLocalErrorDirty(field: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.invalid && control.dirty);
  }

  getLocalError(field: string): string {
    const control = this.form.get(field);
    if (control?.errors) {
      if (control.errors['required']) return 'Este campo es obligatorio';
      if (control.errors['email']) return 'Email inválido';
      
      // Errores de Razón Social
      if (field === 'razonSocial') {
        if (control.errors['formatoInvalido']) {
          return 'Solo se permiten letras, números, espacios, puntos y comas';
        }
        if (control.errors['maxlength']) {
          return 'La Razón Social no puede exceder 20 caracteres';
        }
      }

      // Errores de NIT
      if (field === 'nit') {
        if (control.errors['formatoInvalido']) {
          return 'El NIT solo debe contener números';
        }
        if (control.errors['minimoDigitos']) {
          return 'El NIT debe tener al menos 7 dígitos';
        }
        if (control.errors['maximoDigitos']) {
          return 'El NIT no puede tener más de 13 dígitos';
        }
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
}
