import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HabitacionesListComponent } from './habitaciones-list.component';
import { HabitacionService } from '../../../core/services/habitacion.service';
import { of } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router'; // Importa ActivatedRoute
import { OrderByNumeroPipe } from './order-by-numero.pipe';

describe('HabitacionesListComponent (Prueba Unitaria)', () => {
  let component: HabitacionesListComponent;
  let fixture: ComponentFixture<HabitacionesListComponent>;
  
  const habitacionServiceMock = jasmine.createSpyObj('HabitacionService', [
    'getHabitaciones', 
    'getTiposHabitacion'
  ]);

  const routerMock = jasmine.createSpyObj('Router', ['navigate']);

  // SIMULACIÓN DE RUTAS (Mock para ActivatedRoute)
  const activatedRouteMock = {
    queryParams: of({}) // Simulamos que no hay parámetros de búsqueda por ahora
  };

  beforeEach(async () => {
    habitacionServiceMock.getHabitaciones.and.returnValue(of([
      { id: 1, numero: '101', piso: '1', tipoNombre: 'Simple', capacidad: 1, estado: 'Libre' },
      { id: 2, numero: '202', piso: '2', tipoNombre: 'Doble', capacidad: 2, estado: 'Ocupada' }
    ]));
    habitacionServiceMock.getTiposHabitacion.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [HabitacionesListComponent, FormsModule, OrderByNumeroPipe],
      providers: [
        { provide: HabitacionService, useValue: habitacionServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock } // <--- AGREGAMOS ESTO
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HabitacionesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); 
  });

  it('debería crearse el componente correctamente', () => {
    expect(component).toBeTruthy();
  });

  it('debería filtrar las habitaciones por número correctamente', () => {
    component.busquedaNumero = '101';
    const filtradas = component.habitacionesFiltradas;
    
    expect(filtradas.length).toBe(1);
    expect(filtradas[0].numero).toBe('101');
  });

  it('debería abrir el modal de edición con los datos correctos', () => {
    const habitacionPrueba = { id: 99, numero: '999', estado: 'Libre' };
    component.abrirModalEditar(habitacionPrueba);
    
    expect(component.modalEditarAbierto).toBeTrue();
    expect(component.habitacionAEditar.id).toBe(99);
  });
});