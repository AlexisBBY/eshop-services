namespace Order.API.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = "OrdersDb";
        public string CollectionName { get; set; } = "orders";
    }
}