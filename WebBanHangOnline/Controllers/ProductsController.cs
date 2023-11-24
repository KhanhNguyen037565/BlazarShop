using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;

namespace WebBanHangOnline.Controllers
{
    public class ProductsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        // GET: Products
        public ActionResult Index()
        {
            var items = db.Products.ToList();
            
            return View(items);
        }

        public ActionResult Detail(string alias,int id)
        {
            var item = db.Products.Find(id);
            if (item != null)
            {
                db.Products.Attach(item);
                item.ViewCount = item.ViewCount + 1;
                db.Entry(item).Property(x => x.ViewCount).IsModified = true;
                db.SaveChanges();
            }
            
            return View(item);
        }
        public ActionResult ProductCategory(string alias,int id)
        {
            var items = db.Products.ToList();
            if (id > 0)
            {
                items = items.Where(x => x.ProductCategoryId == id).ToList();
            }
            var cate = db.ProductCategories.Find(id);
            if (cate != null)
            {
                ViewBag.CateName = cate.Title;
            }

            ViewBag.CateId = id;
            return View(items);
        }

        public ActionResult Partial_ItemsByCateId()
        {
            var items = db.Products.Where(x => x.IsHome && x.IsActive).Take(12).ToList();
            return PartialView(items);
        }
        

        
        public ActionResult Partial_ProductSales()
        {
            var items = db.Products.Where(x => x.IsSale && x.IsActive).Take(12).ToList();
            return PartialView(items);
        }
        public ActionResult Partial_ProductSimilars(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            // Tìm các sản phẩm tương tự dựa trên các thuộc tính
            var similarProducts = db.Products.AsEnumerable()
                .Where(p => p.Id != product.Id)
                .Select(p => new
                {
                    Product = p,
                    Similarity = GetCosineSimilarity(product, p)
                }).Where(c => c.Similarity > 0.3) // Lấy những sản phẩm có độ tương đồng trên 0.3
                .OrderByDescending(c => c.Similarity).Select(c => c.Product).Take(7).ToList();

            
            return PartialView(similarProducts);

        }

        // Tính cosine similarity giữa hai sản phẩm
        private double GetCosineSimilarity(Product a, Product b)
        {
            var dictA = new Dictionary<string, double>
            {
                { "Alias_" + a.Alias, 2.0 },
                { "Price_" + a.Price, 2.0 }
            };

            var dictB = new Dictionary<string, double>
            {
                { "Alias_" + b.Alias, 2.0 },
                { "Price_" + b.Price, 2.0 }
            };

            var innerProduct = dictA.Join(dictB,
                d => d.Key,
                d => d.Key,
                (d1, d2) => d1.Value * d2.Value).Sum();

            var normA = dictA.Values.Sum(v => v * v);
            var normB = dictB.Values.Sum(v => v * v);

            return innerProduct / Math.Sqrt(normA * normB);
        }


    }
}