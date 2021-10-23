using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Taste.DataAccess.Data.Repository.IRepository;
using Taste.Models;
using Taste.Models.ViewModels;
using Taste.Utility;

namespace Taste.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [HttpGet]
        public IActionResult Get()
        {
            List<OrderDetailsViewModel> orderListVM = new List<OrderDetailsViewModel>();
            IEnumerable<OrderHeader> orderHeaderList;
            if (User.IsInRole(SD.CustomerRole))
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(u => u.UserId == claim.Value, null, "ApplicationUser");
            }
            else
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(null, null, "ApplicationUser");
            }
            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = _unitOfWork.OrderDetail.GetAll(o => o.OrderId == item.Id).ToList()
                };
                orderListVM.Add(individual);
            }

            return null;
        }
    }
}
