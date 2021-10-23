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
using Taste.Utility;
using Microsoft.AspNetCore.Http;
using Stripe;

namespace Taste.Pages.Customer.Cart
{
    public class SummaryModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public SummaryModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [BindProperty]
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

            OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            OrderDetailsCartVM.OrderHeader.OrderDate = DateTime.Now;
            OrderDetailsCartVM.OrderHeader.UserId = claim.Value;
            OrderDetailsCartVM.OrderHeader.Status = SD.PaymentStatusPending;
            OrderDetailsCartVM.OrderHeader.PickUpTime = 
                Convert.ToDateTime(OrderDetailsCartVM.OrderHeader.PickUpDate.ToShortDateString() + " " + OrderDetailsCartVM.OrderHeader.PickUpTime.ToShortTimeString());

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _unitOfWork.OrderHeader.Add(OrderDetailsCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var item in OrderDetailsCartVM.listCart)
            {
                item.MenuItem = _unitOfWork.MenuItem.GetFirstOrDefault(m => m.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = OrderDetailsCartVM.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                OrderDetailsCartVM.OrderHeader.OrderTotal += (orderDetails.Count * orderDetails.Price);
                _unitOfWork.OrderDetail.Add(orderDetails);
            }
            OrderDetailsCartVM.OrderHeader.OrderTotal = Convert.ToDouble(String.Format("{0:.##}", OrderDetailsCartVM.OrderHeader.OrderTotal));
            _unitOfWork.ShoppingCart.RemoveRange(OrderDetailsCartVM.listCart);
            HttpContext.Session.SetInt32(SD.ShoppingCart,0);
            _unitOfWork.Save();

            if (stripeToken != null)
            {
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(OrderDetailsCartVM.OrderHeader.OrderTotal * 1000),
                    Currency = "usd",
                    Description = "Order ID : " + OrderDetailsCartVM.OrderHeader.Id,
                    Source = stripeToken
                };
                var service = new ChargeService();
                Charge charge = service.Create(options);
                OrderDetailsCartVM.OrderHeader.TransactionId = charge.Id;
                if (charge.Status.ToLower() == "succeeded")
                {
                    OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    OrderDetailsCartVM.OrderHeader.Status = SD.StatusSubmmitted;
                }
                else
                {
                    OrderDetailsCartVM.OrderHeader.Status = SD.PaymentStatusRejected;

                }
            }
            else
            {
                OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            _unitOfWork.Save();
            return RedirectToPage("/Customer/Cart/OrderConfirmation", new { id = OrderDetailsCartVM.OrderHeader.Id });
        }
    }
}
