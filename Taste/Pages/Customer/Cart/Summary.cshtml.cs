using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Taste.DataAccess.Data.Repository.IRepository;
using Taste.Models.ViewModels;
using Taste.Models;

namespace Taste.Pages.Customer.Cart
{
    public class SummaryModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public SummaryModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public OrderDetailsCart OrderDetailsCartVM { get; set; }
        public IActionResult OnGet()
        {
            OrderDetailsCartVM = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            OrderDetailsCartVM.OrderHeader.OrderTotal = 0;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IEnumerable<ShoppingCart> cart = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == claim.Value);
            if (cart != null)
            {
                OrderDetailsCartVM.listCart = cart.ToList();
            }

            foreach (var cartList in OrderDetailsCartVM.listCart)
            {
                cartList.MenuItem = _unitOfWork.MenuItem.GetFirstOrDefault(m => m.Id == cartList.MenuItemId);
                OrderDetailsCartVM.OrderHeader.OrderTotal += cartList.MenuItem.Price * cartList.Count;
            }

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(c => c.Id == claim.Value);
            OrderDetailsCartVM.OrderHeader.PickupName = applicationUser.FullName;
            OrderDetailsCartVM.OrderHeader.PickUpTime = DateTime.Now;
            OrderDetailsCartVM.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            return Page();
        }
        public IActionResult OnPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            OrderDetailsCartVM.listCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value).ToList();
        }
    }
}
