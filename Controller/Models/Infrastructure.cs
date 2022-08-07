using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller.Models
{
    internal class Infrastructure
    {
    }

    public static class EasyNetQHelper
    {
        public static string GetSubscription<TMessage, TSubscriber>() => $"{typeof(TMessage).Name}-{typeof(TSubscriber).Name}";
    }
}
