import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = 'https://localhost:5100'; // Replace with your API URL

  constructor(private http: HttpClient) { }

  getProducts(id: string): Observable<any> {
    id = "All"
    return this.http.get(`${this.apiUrl}/products?requestType=0&id=${id}`);
  }

  getProductDetails(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/products?requestType=1&id=${id}`);
  }
}
