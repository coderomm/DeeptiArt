using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DeeptiArt.Models;

namespace DeeptiArt.Controllers
{
    public class baseController : Controller
    {
        dbdeeptiartsEntities db = new dbdeeptiartsEntities();

        protected List<CartProductViewModel> CartItems { get; private set; }

        protected int CartItemCount { get; private set; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            int userId = Convert.ToInt32(Session["userid"]);
            ViewBag.cSliderImage1 = db.AboutDetailsTbls.SingleOrDefault().cSliderImage1;
            ViewBag.cSliderImage2 = db.AboutDetailsTbls.SingleOrDefault().cSliderImage2;
            ViewBag.cSliderImage3 = db.AboutDetailsTbls.SingleOrDefault().cSliderImage3;

            ViewBag.AboutDetails = db.AboutDetailsTbls.SingleOrDefault();

            ViewBag.Gallery = db.ProductTbls.OrderByDescending(x => x.rts).ToList();
            ViewBag.Products = db.ProductTbls.OrderByDescending(x => x.rts).ToList();
            ViewBag.HomeBannerSlider = db.HomeBannerSliderTbls.ToList();

            var SketchCategoryProducts15 = db.ProductTbls.Where(x => x.CId == 1).OrderByDescending(u => u.rts).Take(15).OrderBy(x => Guid.NewGuid()).ToList();
            ViewBag.SketchCategoryProducts15 = SketchCategoryProducts15.Any() ? SketchCategoryProducts15 : new List<ProductTbl>();

            var MandalaCategoryProducts15 = db.ProductTbls.Where(x => x.CId == 2).OrderByDescending(u => u.rts).Take(15).OrderBy(x => Guid.NewGuid()).ToList();
            ViewBag.MandalaCategoryProducts15 = MandalaCategoryProducts15.Any() ? MandalaCategoryProducts15 : new List<ProductTbl>();

            var ColouredCategoryProducts15 = db.ProductTbls.Where(x => x.CId == 3).OrderByDescending(u => u.rts).Take(15).OrderBy(x => Guid.NewGuid()).ToList();
            ViewBag.ColouredCategoryProducts15 = ColouredCategoryProducts15.Any() ? ColouredCategoryProducts15 : new List<ProductTbl>();
            
            ViewBag.SketchCategoryCount = db.ProductTbls.Where(x => x.CId == 1).Count();
            ViewBag.MandalaCategoryCount = db.ProductTbls.Where(x => x.CId == 2).Count();
            ViewBag.ColouredCategoryCount = db.ProductTbls.Where(x => x.CId == 3).Count();

            ViewBag.MainCategoryTbls = db.MainCategoryTbls.OrderByDescending(x => x.Id).ToList();
            ViewBag.SubCategoryTbls = db.SubCategoryTbls.Where(subcat => db.ProductTbls.Any(item => item.SCId == subcat.Id)).ToList();

            ViewBag.OurTeam = db.OurTeamTbls.ToList();
            ViewBag.FrameTbls = db.FrameTbls.ToList();
            ViewBag.ReviewTables = db.ReviewTables.OrderByDescending(x => x.rts).ToList();
            ViewBag.ReviewTables10 = db.ReviewTables.OrderBy(x => x.Id).Take(7).ToList();

            ViewBag.ReviewCount = db.ReviewTables.Count();
            CartItems = GetCartItemsForCurrentUser();
            ViewBag.CartTbls = CartItems;
            ViewBag.WishlistTblsCount = db.WishlistTbls.Where(x => x.CustomerID == userId).Count();
            base.OnActionExecuting(filterContext);
        }

        public JsonResult GetNavratriCount()
        {
            var count = db.ProductTbls.Where(x => x.CId == 4).Count();
            return Json(count, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetMyOrdersCount()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            var myordersCount = db.OrderTbls.Where(x => x.CustomerID == userId).Count();
            return Json(myordersCount, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetProductCount()
        {
            int GetProductCount = db.ProductTbls.Count();
            return Json(GetProductCount, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCartItemCount()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            int CartItemCount = db.CartTbls.Where(x => x.CustomerID == userId).Count();
            return Json(CartItemCount, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetWishlistItems()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            var items = db.WishlistTbls.Where(x => x.CustomerID == userId).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetWishlistItemsCount()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            int count = db.WishlistTbls.Where(x => x.CustomerID == userId).Count();
            return Json(count, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCartItems()
        {
            List<CartProductViewModel> cartItems = GetCartItemsForCurrentUser();
            return Json(cartItems, JsonRequestBehavior.AllowGet);
        }

        private List<CartProductViewModel> GetCartItemsForCurrentUser()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            var query = from cart in db.CartTbls
                        join product in db.ProductTbls on cart.ProductID equals product.Id
                        join user in db.RegisteredUsersTbls on cart.CustomerID equals user.Id
                        join frame in db.FrameTbls on cart.FrameID equals frame.Id
                        where cart.CustomerID == userId
                        select new CartProductViewModel
                        {
                            CartTbl = cart,
                            ProductTbl = product,
                            UserTbl = user,
                            FrameTbl = frame
                        };

            var cartItems = query.ToList();
            return cartItems;
        }
    }
}
