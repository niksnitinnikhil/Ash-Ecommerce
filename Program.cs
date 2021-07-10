using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecommerce
{
    public class Cart : List<CartItem>
    {
        private readonly Dictionary<string, IDiscount> _offers = new Dictionary<string, IDiscount>(StringComparer.OrdinalIgnoreCase);

        public Cart() : base(new List<CartItem>())
        {
        }

        public CartItem AddCartItem(Product product, int quantity)
        {
            var cartItem = this.Find(c => c.Product.Equals(product));
            if (cartItem is null)
            {
                cartItem = new CartItem(product, quantity);
                this.Add(cartItem);
                return cartItem;
            }
            cartItem.Quantity += quantity;
            return cartItem;
        }

        public void AddDiscount(params IDiscount[] offer)
        {
            foreach (var f in offer)
                _offers.TryAdd(f.UniqueKey, f);
        }

        public decimal Total
        {
            get
            {
                return this.Sum(c => c.PayablePrice);
            }
        }

        public void ApplyDiscountOnCartItem()
        {
            foreach (var f in _offers)
            {
                f.Value.DiscountApply(this);
            }
        }

        public override string ToString()
        {
            var ct = new ConsoleTable("Product", "Price", "Quanity", "Discount", "Applied Discount", "Payable Price");
            foreach (var c in this)
                ct.AddRow(c.Product.Name, c.Product.Price.ToString("0.00"), c.Quantity, c.DiscountPrice.ToString("0.00"), string.Join(Environment.NewLine, c.GetAllAppliedDiscount().Select(c => c.Name)), c.PayablePrice.ToString("0.00"));
            ct.AddRow(string.Empty, string.Empty, string.Empty, string.Empty, "Total Cart Price:", this.Total.ToString("0.00"));
            return ct.ToString();
        }
    }

    public class CartItem
    {
        private readonly Dictionary<string, IDiscount> _offers = new Dictionary<string, IDiscount>(StringComparer.OrdinalIgnoreCase);

        public CartItem(Product product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }

        public Product Product { get; }
        public int Quantity { get; set; }

        public decimal DiscountPrice { get; set; }

        public decimal PayablePrice
        {
            get
            {
                return (Product.Price * Quantity) - DiscountPrice;
            }
        }

        public void AddOffer(IDiscount discount)
        {
            _offers.TryAdd(discount.UniqueKey, discount);
        }

        public IReadOnlyCollection<IDiscount> GetAllAppliedDiscount()
        {
            return _offers.Values;
        }
    }

    public class Product : IEquatable<Product>
    {
        public string Name { get; }
        public decimal Price { get; }

        public Product(string name, decimal price)
        {
            Name = name;
            Price = price;
        }

        public bool Equals(Product product) => this.Name == product.Name;
    }

    public interface IDiscount
    {
        public string Name { get; }
        public string UniqueKey { get; }
        public void DiscountApply(IEnumerable<CartItem> cartItem);

    }

    public class BuyNItemAndXFree : IDiscount
    {
        public BuyNItemAndXFree(string name, Product product, int n, int x)
        {
            Name = name;
            Product = product;
            N = n;
            X = x;
        }

        public string Name { get; }

        public string UniqueKey => string.Concat(Name, Product.Name);

        private Product Product { get; }
        private int N { get; }
        private int X { get; }

        public void DiscountApply(IEnumerable<CartItem> cartItems)
        {
            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product.Equals(Product) && cartItem.Quantity > N)
                {
                    cartItem.DiscountPrice = (cartItem.Quantity - (cartItem.Quantity - Math.Floor((decimal)cartItem.Quantity / (X + N)))) * cartItem.Product.Price;
                    cartItem.AddOffer(this);
                }
            }
        }
    }

    public class BuyTypeOfItemAndSpecificItemFree : IDiscount
    {
        public BuyTypeOfItemAndSpecificItemFree(string name, Action<List<Product>> needToBuy, Action<List<Product>> freeItem)
        {
            NeedToBuy = new List<Product>();
            FreeItem = new List<Product>();
            Name = name;
            needToBuy(NeedToBuy);
            freeItem(FreeItem);
        }

        public string Name { get; }

        public string UniqueKey { get { return Guid.NewGuid().ToString(); } }
        private List<Product> NeedToBuy { get; }
        private List<Product> FreeItem { get; }

        public void DiscountApply(IEnumerable<CartItem> cartItem)
        {
            if (NeedToBuy.All(c => cartItem.Any(x => x.Product.Equals(c))))
            {
                foreach (var item in cartItem.Where(c => FreeItem.Any(x => x.Equals(c.Product))))
                {
                    item.DiscountPrice = item.Product.Price;
                    item.AddOffer(this);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var cart = new Cart();

            var apple = new Product("Apple", 200);
            cart.AddDiscount(new BuyNItemAndXFree("Buy two, get one free", apple, 2, 1));
            cart.AddDiscount(new BuyNItemAndXFree("Buy two, get one free", apple, 2, 1));
            cart.AddCartItem(apple, 1);
            cart.AddCartItem(apple, 2);

            var orange = new Product("Orange", 100);
            cart.AddCartItem(orange, 3);

            var banana = new Product("Banana", 100);
            cart.AddDiscount(new BuyNItemAndXFree("Buy two, get one free", banana, 2, 1));
            cart.AddCartItem(banana, 5);



            var redShirt = new Product("Red Shirt", 300);
            cart.AddCartItem(redShirt, 1);



            var whiteShirt = new Product("White Shirt", 200);
            cart.AddCartItem(whiteShirt, 1);

            var blackShirt = new Product("Black Shirt", 300);
            cart.AddCartItem(blackShirt, 1);


            cart.AddDiscount(new BuyTypeOfItemAndSpecificItemFree("Buy Red & White Shirt , Get Only One Black Shirt free", c =>
            {
                c.Add(redShirt);
                c.Add(whiteShirt);
            },
            c => c.Add(blackShirt)));


            cart.ApplyDiscountOnCartItem();

            Console.WriteLine(cart);
            Console.ReadLine();
        }
    }
}
