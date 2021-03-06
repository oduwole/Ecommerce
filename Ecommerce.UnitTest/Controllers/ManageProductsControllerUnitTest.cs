﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Routing;
using Ecommerce.Data.Infrastructure;
using Ecommerce.Data.Repositories;
using Ecommerce.Domain.DTO;
using Ecommerce.Domain.Entities;
using Ecommerce.Service;
using Ecommerce.Service.Infrastructure;
using Ecommerce.Web.Areas.Admin.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ecommerce.UnitTest.Controllers
{
    [TestClass]
    public class ManageProductsControllerUnitTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IProductRepository> _productRepositoryMock;
        private IRepositoryService<Product> _productService;
        private IProductRepository _productRepository;


        [TestInitialize]
        public void Initialize()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _productService = new ProductService(_productRepositoryMock.Object, _unitOfWorkMock.Object);

            _productRepository = _productRepositoryMock.Object;

            var category1 = new Category()
            {
                Name = "Toys"

            };

            var category2 = new Category()
            {
                Name = "Food"

            };

            var category3 = new Category()
            {
                Name = "Tools"

            };
            var products = new List<Product>()            
            {
                new Product() {Id = 1, Name = "Tomato Soup", Price = 1.39M, ActualCost = .99M, Category =  category2},
                new Product() {Id = 2, Name = "Hammer", Price = 16.99M, ActualCost = 10, Category =  category3},
                new Product() {Id = 3, Name = "Yo yo", Price = 6.99M, ActualCost = 2.05M, Category =  category1},
                new Product() {Id = 4, Name = "Po Po", Price = 3.99M, ActualCost = 2.05M, Category =  category1 }

            };

            _productRepositoryMock.Setup(m => m.Add(It.IsAny<Product>())).Callback((Product product) =>
            {
                product.Id = products.Count + 1;
                products.Add(product);
            });


            _productRepositoryMock.Setup(m => m.GetAll()).Returns(products);

            _productRepositoryMock.Setup(m => m.Get(It.IsAny<Expression<Func<Product, bool>>>())).Returns((Expression<Func<Product, bool>> expression) =>
            {
                var data = products.Where(expression.Compile()).FirstOrDefault();
                return data;
            });

            _productRepositoryMock.Setup(m => m.Update(It.IsAny<Product>())).Callback(((Product product) =>
            {
                var t = products.Single(p => p.Id == product.Id);
                if (t != null)
                {
                    t.Id = product.Id;
                    t.Name = product.Name;
                    t.ActualCost = product.ActualCost;
                    t.Price = product.Price;
                    t.CategoryId = product.CategoryId;   
                }
               
            })).Verifiable();


            _productRepositoryMock.Setup(m => m.Delete(It.IsAny<Product>())).Callback(((Product p) =>
            {
                products.Remove(p);
            }));

            _productRepositoryMock.Setup(m => m.GetById(It.IsAny<long>())).Returns((long id) =>
            {
                var product = products.FirstOrDefault(i => i.Id == id);
                return product;
            });

            _unitOfWorkMock.Setup(i => i.Commit()).Callback(() =>
            {

            });

        }
        [TestMethod]
        public void TestMethod1()
        {
            var controller = new ManageProductsController(_productService)
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
           

            var response = controller.GetProducts();

            Assert.IsNotNull(response);
        }

         [TestMethod]
        public void TestMethod2()
        {

             //Arrange
            var controller = new ManageProductsController(_productService)
            {
                Request = new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://localhost/api/products")
                },
                Configuration = new HttpConfiguration()
            };
            controller.Configuration.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
            );

            controller.RequestContext.RouteData = new HttpRouteData(
            route: new HttpRoute(),
            values: new HttpRouteValueDictionary { { "controller", "products" } });

             //Arrange
            var count = _productRepository.GetAll().Count();
             var product = new ProductDto()
             {
                 Id = 1,
                 Name = "Tomato Soup",
                 Price = 1.39M,
                 ActualCost = .99M,
             };
            var response = controller.Post(product);

             

             //
            Assert.IsNotNull(response);

            Assert.AreNotEqual(count, _productRepository.GetAll().Count());
            
            Assert.AreEqual("http://localhost/api/products/5", response.Headers.Location.AbsoluteUri);
        }

         [TestMethod]
         public void TestMethod3()
         {

             var client = new HttpClient()
             {
                 BaseAddress = new Uri("http://localhost:1857"),
                 DefaultRequestHeaders = { }

             };

             var dto = new OrderDto()
             {
                 Details = new List<ProductDetailDto>()
            {
                new ProductDetailDto(){Product = "Sugar", Price = 200, ProductId = 1009, Quantity = 1},
                new ProductDetailDto(){Product = "Bread", Price = 200, ProductId = 1010, Quantity = 1}
            }
             };

             client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
             client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                 Convert.ToBase64String(
                     System.Text.Encoding.ASCII.GetBytes(
                         string.Format("{0}:{1}", "admin@ecommerce.ng", "Test123456"))));

             //var response = client.PostAsJsonAsync("api/products", dto).Result;
             var response = client.GetAsync("api/ManageProducts").Result;

             var products = response.Content.ReadAsAsync<IEnumerable<ProductDto>>().Result;

             Assert.IsNotNull(products);
             
             Assert.IsTrue(response.IsSuccessStatusCode);

             
         }
    }
}
