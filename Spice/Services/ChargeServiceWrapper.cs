using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Services
{
    public interface IChargeServiceWrapper
    {
        Charge Create(ChargeCreateOptions options);
    }
    public class ChargeServiceWrapper : IChargeServiceWrapper
    {
        public Charge Create(ChargeCreateOptions options)
        {
            var service = new Stripe.ChargeService();
            return service.Create(options);
        }
    }
}
