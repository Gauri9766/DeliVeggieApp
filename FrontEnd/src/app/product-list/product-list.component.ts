import { Component, OnInit } from '@angular/core';
import { ProductService } from '../product.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css']
})
export class ProductListComponent implements OnInit {
  products: any[] = [];

  constructor(private productService: ProductService, private router: Router) { }

  ngOnInit(): void {
    this.productService.getProducts("id").subscribe(data => {
      this.products = Array.isArray(data) ? data : [];
      
    });
  }

  viewDetails(productId: string): void {
    this.router.navigate(['/product', productId]);
  }
}
