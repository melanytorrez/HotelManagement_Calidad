import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors as NgValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { HuespedService } from '../../core/services/huesped.service';
import { finalize, map, debounceTime, distinctUntilChanged, first } from 'rxjs';

interface ValidationErrors {
  [key: string]: string[];
}

@Component({
  selector: 'app-nuevo-huesped',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './nuevo-huesped.component.html',
  styleUrls: ['./nuevo-huesped.component.scss']
})
export class NuevoHuespedComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly service = inject(HuespedService);

  submitting = signal(false);
  mensaje = signal<string | null>(null);
  error = signal<string | null>(null);
  
  // Errores de validación del backend por campo
  serverErrors = signal<ValidationErrors>({});

  form = this.fb.nonNullable.group({
    primerNombre: ['', [
      Validators.required, 
      Validators.minLength(2),
      Validators.maxLength(30),
      this.soloLetrasValidator()
    ]],
    segundoNombre: ['', [
      Validators.maxLength(30),
      this.soloLetrasValidator()
    ]],
    primerApellido: ['', [
      Validators.required, 
      Validators.minLength(2),
      Validators.maxLength(30),
      this.soloLetrasValidator()
    ]],
    segundoApellido: ['', [
      Validators.maxLength(30),
      this.soloLetrasValidator()
    ]],
    documento: ['', 
      [
        Validators.required, 
        Validators.minLength(5), 
        Validators.maxLength(20),
        this.soloNumerosValidator()
      ],
      [this.documentoExistenteValidator()]
    ],
    telefono: ['', [
      Validators.minLength(7),
      Validators.maxLength(20),
      this.telefonoFormatoValidator()
    ]],
    fechaNacimiento: ['', [this.fechaValidaValidator()]]
  });

  ngOnInit(): void {
    // Validación en tiempo real - actualizar validación mientras se escribe
    this.form.valueChanges.pipe(
      debounceTime(300) // Esperar 300ms después de que el usuario deje de escribir
    ).subscribe(() => {
      // Limpiar errores del servidor cuando el usuario modifique el campo
      if (this.serverErrors() && Object.keys(this.serverErrors()).length > 0) {
        this.serverErrors.set({});
      }
    });
  }

  // Validador personalizado: solo letras (y espacios, acentos, ñ)
  private soloLetrasValidator() {
    return (control: AbstractControl): NgValidationErrors | null => {
      if (!control.value || control.value.trim() === '') {
        return null; // No validar si está vacío (lo maneja required)
      }
      const regex = /^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$/;
      return regex.test(control.value) ? null : { soloLetras: true };
    };
  }

  // Validador personalizado: solo números
  private soloNumerosValidator() {
    return (control: AbstractControl): NgValidationErrors | null => {
      if (!control.value || control.value.trim() === '') {
        return null; // No validar si está vacío (lo maneja required)
      }
      const regex = /^\d+$/;
      return regex.test(control.value) ? null : { soloNumeros: true };
    };
  }

  // Validador personalizado: formato de teléfono
  private telefonoFormatoValidator() {
    return (control: AbstractControl): NgValidationErrors | null => {
      if (!control.value || control.value.trim() === '') {
        return null;
      }
      const regex = /^[0-9+\-\s()]+$/;
      return regex.test(control.value) ? null : { formatoTelefono: true };
    };
  }

  // Validador personalizado: fecha válida
  private fechaValidaValidator() {
    return (control: AbstractControl): NgValidationErrors | null => {
      if (!control.value) {
        return null;
      }
      const fecha = new Date(control.value);
      const hoy = new Date();
      
      // No puede ser futura
      if (fecha > hoy) {
        return { fechaFutura: true };
      }
      
      // No puede ser mayor a 150 años
      const edadMaxima = new Date();
      edadMaxima.setFullYear(edadMaxima.getFullYear() - 150);
      if (fecha < edadMaxima) {
        return { fechaMuyAntigua: true };
      }
      
      return null;
    };
  }

  // Validador asíncrono: verificar si el documento ya existe
  private documentoExistenteValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value || control.value.trim() === '') {
        return Promise.resolve(null);
      }
      
      return this.service.checkDocumentoExists(control.value).pipe(
        debounceTime(500), // Esperar 500ms después de escribir
        distinctUntilChanged(),
        map(exists => exists ? { documentoExistente: true } : null),
        first()
      );
    };
  }

  cancelar(): void {
    this.router.navigate(['/huespedes']);
  }

  guardar(): void {
    this.mensaje.set(null);
    this.error.set(null);
    this.serverErrors.set({}); // Limpiar errores anteriores

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Por favor completa los campos obligatorios correctamente.');
      return;
    }

    this.submitting.set(true);

    // Extraer valores con non-null assertion para evitar undefined
    const primerNombre: string = this.form.get('primerNombre')!.value.trim();
    const segundoNombre: string = (this.form.get('segundoNombre')!.value || '').trim();
    const primerApellido: string = this.form.get('primerApellido')!.value.trim();
    const segundoApellido: string | null = (this.form.get('segundoApellido')!.value || '').trim() || null;
    const documento: string = this.form.get('documento')!.value;
    const telefono: string | null = this.form.get('telefono')!.value || null;
    const fechaNacimiento: string | null = this.form.get('fechaNacimiento')!.value || null;

    // Construir Nombre completo para el backend: incluir segundoNombre si existe
    const Nombre = segundoNombre ? `${primerNombre} ${segundoNombre}` : primerNombre;
    const Apellido = primerApellido;
    const Segundo_Apellido = segundoApellido;

    const payload = {
      Nombre,
      Apellido,
      Segundo_Apellido,
      Documento_Identidad: documento,
      Telefono: telefono,
      Fecha_Nacimiento: fechaNacimiento
    };

    this.service.createHuesped(payload).pipe(
      finalize(() => this.submitting.set(false))
    ).subscribe({
      next: () => {
        this.mensaje.set('✅ Huésped creado correctamente.');
        setTimeout(() => this.router.navigate(['/huespedes']), 900);
      },
      error: (err: any) => {
        console.log('❌ Error completo:', err);
        console.log('📋 Error response:', err?.error);
        console.log('🔍 Errores de validación:', err?.error?.errors);
        
        // Verificar si hay errores de validación del backend
        if (err?.error?.errors) {
          // ⚠️ IMPORTANTE: El backend envía las claves en camelCase
          // documento_Identidad → documentoIdentidad
          // fecha_Nacimiento → fechaNacimiento  
          // segundo_Apellido → segundoApellido
          console.log('📋 Claves de errores recibidas del backend:', Object.keys(err.error.errors));
          
          this.serverErrors.set(err.error.errors);
          this.error.set('❌ Por favor corrige los errores marcados en el formulario.');
          
          // Log para debugging: mostrar qué errores se detectaron
          Object.keys(err.error.errors).forEach(key => {
            console.log(`  ✓ Campo "${key}":`, err.error.errors[key]);
          });
        } else {
          const msg = err?.error?.message ?? err?.message ?? 'Error al crear huésped';
          this.error.set(`❌ ${msg}`);
        }
      }
    });
  }

  /**
   * Obtiene el primer error del servidor para un campo específico
   */
  getServerError(fieldName: string): string | null {
    const errors = this.serverErrors();
    if (errors[fieldName] && errors[fieldName].length > 0) {
      return errors[fieldName][0];
    }
    return null;
  }

  /**
   * Verifica si un campo tiene errores del servidor
   */
  hasServerError(fieldName: string): boolean {
    const errors = this.serverErrors();
    return !!(errors[fieldName] && errors[fieldName].length > 0);
  }

  /**
   * Obtiene el mensaje de error local del formulario
   */
  getLocalError(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control?.errors || !control?.touched) {
      return null;
    }

    const errors = control.errors;
    
    if (errors['required']) {
      return 'Este campo es obligatorio';
    }
    if (errors['minlength']) {
      const minLength = errors['minlength'].requiredLength;
      return `Debe tener al menos ${minLength} caracteres`;
    }
    if (errors['maxlength']) {
      const maxLength = errors['maxlength'].requiredLength;
      return `No debe exceder ${maxLength} caracteres`;
    }
    if (errors['soloLetras']) {
      return 'Solo se permiten letras';
    }
    if (errors['soloNumeros']) {
      return 'Solo se permiten números';
    }
    if (errors['formatoTelefono']) {
      return 'Formato de teléfono inválido';
    }
    if (errors['fechaFutura']) {
      return 'La fecha no puede ser futura';
    }
    if (errors['fechaMuyAntigua']) {
      return 'La fecha no puede ser mayor a 150 años';
    }
    if (errors['documentoExistente']) {
      return '⚠️ Este documento ya está registrado';
    }
    
    return null;
  }

  /**
   * Verifica si un campo tiene errores locales
   */
  hasLocalError(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!(control && control.invalid && control.touched);
  }
}
