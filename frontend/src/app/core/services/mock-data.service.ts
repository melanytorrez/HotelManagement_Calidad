import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { ReservaLite } from '../../shared/models/reserva-lite.model';
import { HabitacionLite } from '../../shared/models/habitacion-lite.model';
import { HuespedLite } from '../../shared/models/huesped-lite.model';

@Injectable({
  providedIn: 'root'
})
export class MockDataService {
  private readonly habitacionesMock: HabitacionLite[] = [
    {
      id: '1',
      numero: '101',
      piso: 1,
      tipoNombre: 'Simple',
      capacidad: 1,
      tarifaBase: 180.00,
      estado: 'Libre'
    },
    {
      id: '2',
      numero: '102',
      piso: 1,
      tipoNombre: 'Doble',
      capacidad: 2,
      tarifaBase: 260.00,
      estado: 'Ocupada'
    },
    // Add more mock rooms as needed
  ];

  constructor(private readonly http: HttpClient) {}

  getReservas(): Observable<ReservaLite[]> {
    return this.http.get<ReservaLite[]>('assets/data/reservas.json');
  }

  getHabitaciones(): Observable<HabitacionLite[]> {
    return of(this.habitacionesMock);
  }

  getHuespedes() {
    return this.http.get<HuespedLite[]>('assets/data/huespedes.json');
  }
}
