using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PustokTemplate.DAL;
using PustokTemplate.Models;
using PustokTemplate.ViewModels;
using System.Security.Claims;

namespace PustokTemplate.Controllers
{
    public class OrderController : Controller
    {
        private readonly PustokDbContext _context;
		private readonly UserManager<AppUser> _userManager;

		public OrderController(PustokDbContext context,UserManager<AppUser> userManager)
        {
            _context = context;
			_userManager = userManager;
		}
        public async Task<IActionResult> Checkout()
        {
            OrderViewModel ovm = new OrderViewModel();
            ovm.CheckoutItems = GenerateCheckoutItems();


            if(User.Identity.IsAuthenticated && User.IsInRole("Member"))
            {
				AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

                ovm.TotalPrice = ovm.CheckoutItems.Any() ? ovm.CheckoutItems.Sum(x => x.Price * x.Count) : 0;

                ovm.Order = new OrderCreateViewModel
                {
                    Address = user.Address,
                    Email = user.Email,
                    FullName = user.FulName,
                    Phone = user.Phone,
                };

            }
                
            else
            {
				ovm.TotalPrice = ovm.CheckoutItems.Any() ? ovm.CheckoutItems.Sum(x => x.Price * x.Count) : 0;
			}

            return View(ovm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateViewModel ovm)
        {
            if(!ModelState.IsValid)
            {
                OrderViewModel vm = new OrderViewModel();
                vm.CheckoutItems = GenerateCheckoutItems();
                vm.Order = ovm;

                return View("Checkout", vm);
            }



            return Json(ovm);
        }

        private List<CheckoutItem> GenerateCheckoutItemsFromDb(string userId)
        {
            return _context.BasketItems.Include(x => x.Book).Where(x => x.AppUserId == userId).Select(x => new CheckoutItem
			{
				Count = x.Count,
				Name = x.Book.Name,
				Price = (decimal)(x.Book.DiscountPercent > 0 ? (x.Book.InitialPrice * (100 - x.Book.DiscountPercent) / 100) : x.Book.InitialPrice)
			}).ToList();
		}
		private List<CheckoutItem> GenerateCheckoutItemsFromCookie()
        {
            List<CheckoutItem> checkoutItems = new List<CheckoutItem>();

			var basketStr = Request.Cookies["basket"];

			if (basketStr != null)
			{
				List<BasketItemCountViewModel> cookieItems = JsonConvert.DeserializeObject<List<BasketItemCountViewModel>>(basketStr);

				foreach (var item in cookieItems)
				{
					Book book = _context.Books.FirstOrDefault(x => x.Id == item.BookId);
					CheckoutItem checkoutItem = new CheckoutItem
					{
						Count = item.Count,
						Name = book.Name,
						Price = (decimal)(book.DiscountPercent > 0 ? (book.InitialPrice * (100 - book.DiscountPercent) / 100) : book.InitialPrice)
					};

					checkoutItems.Add(checkoutItem);

				}
			}

            return checkoutItems;
		}

        public List<CheckoutItem> GenerateCheckoutItems()
        {
            if(User.Identity.IsAuthenticated && User.IsInRole("Member"))
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                return GenerateCheckoutItemsFromDb(userId);
            }

            else
            {
                return GenerateCheckoutItemsFromCookie();
            }
        }

	}
}
