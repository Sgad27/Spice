using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public static class Constants
    {
        public const string DefaultFoodImage = "default_food.png";
        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

        public const string ssShoppingCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";

        public class OrderStatus
        {
            public const string Pending = "Pending";
            public const string Submitted = "Submitted";
            public const string InProcess = "Being Prepared";
            public const string Ready = "Ready for Pickup";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";

            public class Images
            {
                public const string Submitted = "OrderPlaced.png";
                public const string InProcess = "InKitchen.png";
                public const string Ready = "ReadyForPickup.png";
                public const string Completed = "completed.png";
            }
        }

        public class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
        }


    }
}
