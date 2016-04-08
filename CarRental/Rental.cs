using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using DHTMLX.Scheduler;

namespace CarRental
{
    partial class RentalDataContext
    {
    }

    [MetadataType(typeof(OrderMetadata))]
    public partial class Order
    {
    }

    [MetadataType(typeof(CarMetadata))]
    public partial class Car
    {
    }

    [MetadataType(typeof(TypeMetadata))]
    public partial class Type
    {
    }

    public class CarMetadata
    {
        [DHXJson(Ignore=true)]
        public object Orders { get; set; }
        [DHXJson(Ignore = true)]
        public object Type { get; set; }
    }


    public class OrderMetadata
    {
        [DHXJson(Ignore = true)]
        public object Car { get; set; }
    }

    public class TypeMetadata
    {
        [DHXJson(Ignore = true)]
        public object Cars { get; set; }
    }
}
