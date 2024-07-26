using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnviroment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnviroment = webHostEnvironment;
        }


        public IActionResult Index()
        {
            List<Product> objCateogryList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
         
            return View(objCateogryList);
        }

        public IActionResult Get()
        {
            return View();
        }


        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()

                }),
                Product = new Product()
            };
            if(id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u=>u.Id == id, includeProperties : "ProductImages");
                return View(productVM);
            }
            
        }



        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);

                }
                else
                {

                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnviroment.WebRootPath;
                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        { 
                            //creating folder in project for images
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string productPath = @"images\products\product-" + productVM.Product.Id;
                            string FinalPath = Path.Combine(wwwRootPath, productPath);

                            if (!Directory.Exists(FinalPath)) 
                                Directory.CreateDirectory(FinalPath);

                            using (var fileStream = new FileStream(Path.Combine(FinalPath, fileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            ProductImage productImage = new()
                            {
                                ImageUrl = @"\" + productPath + @"\" + fileName,
                                ProductId = productVM.Product.Id,
                            };

                            if(productVM.Product.ProductImages == null)
                                productVM.Product.ProductImages = new List<ProductImage>();

                            productVM.Product.ProductImages.Add(productImage);
                            
                            
                        }

                        _unitOfWork.Product.Update(productVM.Product);
                        _unitOfWork.Save();



                    }
                }                
                TempData["success"] = "Product created/updated successfully";
                return RedirectToAction("Index");

            }

            return View();
        }

        
        public IActionResult DeleteImage(int imageId)
        {
            var ImageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            var productId = ImageToBeDeleted.ProductId;

            if(ImageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(ImageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                            Path.Combine(_webHostEnviroment.WebRootPath,
                            ImageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork.ProductImage.Remove(ImageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";

            }

            return RedirectToAction(nameof(Upsert), new {id = productId });
        }

        #region API CALLS

        [HttpGet]

        public IActionResult GetAll()
        {
            List<Product> objCateogryList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objCateogryList });
        }

        [HttpDelete]

        public IActionResult Delete(int? id)
        {
            var prodctToBeDeleted = _unitOfWork.Product.Get(u=>u.Id == id);
            if (prodctToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting"});
            }

            //creating folder in project for images
            string productPath = @"images\products\product-" + id;
            string FinalPath = Path.Combine(_webHostEnviroment.WebRootPath, productPath);

            if (Directory.Exists(FinalPath))
            {
                string[] filePaths = Directory.GetFiles(FinalPath);
                foreach (string filePath in filePaths )
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(FinalPath);

            }

            _unitOfWork.Product.Remove(prodctToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = false, message = "Delete Successful"});

        }
        #endregion
    }
}
