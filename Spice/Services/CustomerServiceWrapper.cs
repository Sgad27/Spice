using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Services
{
    public interface ICustomerServiceWrapper
    {
        Customer Create(CustomerCreateOptions options);
    }
    public class CustomerServiceWrapper : ICustomerServiceWrapper
    {
        public Customer Create(CustomerCreateOptions options)
        {
            var service = new Stripe.CustomerService();
            return service.Create(options);
        }
    }
}
