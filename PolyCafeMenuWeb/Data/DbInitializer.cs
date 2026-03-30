using Microsoft.EntityFrameworkCore;
using PolyCafeMenuWeb.Models;

namespace PolyCafeMenuWeb.Data
{
    public static class DbInitializer
    {
        public static void Initialize(PolyCafeContext context)
        {
            context.Database.EnsureCreated(); // Ensure DB is created
            EnsureEmployeeEmailColumn(context);

            // Look for any Employees.
            if (context.Employees.Any())
            {
                return;   // DB has been seeded
            }

            var employees = new Employee[]
            {
                new Employee{FullName="Admin Manager", Username="admin", Password="123", Role="Manager", Phone="0901234567", Email="admin@polycafe.local", IsActive=true, CreatedAt=DateTime.Now},
                new Employee{FullName="Alice Cashier", Username="cashier1", Password="123", Role="Cashier", Phone="0909876543", Email="cashier1@polycafe.local", IsActive=true, CreatedAt=DateTime.Now},
                new Employee{FullName="Bob Barista", Username="barista1", Password="123", Role="Barista", Phone="0901112223", Email="barista1@polycafe.local", IsActive=true, CreatedAt=DateTime.Now}
            };
            foreach (Employee e in employees)
            {
                context.Employees.Add(e);
            }
            context.SaveChanges();
            
            if (!context.PaymentMethods.Any())
            {
                context.PaymentMethods.Add(new PaymentMethod { MethodName = "Cash", MethodType = "Cash", IsActive = true });
                context.PaymentMethods.Add(new PaymentMethod { MethodName = "Credit Card", MethodType = "Card", IsActive = true });
                context.SaveChanges();
            }

            // Seed Categories if empty
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { CategoryName = "Coffee", Description = "Espresso based drinks", IsActive = true },
                    new Category { CategoryName = "Tea", Description = "Fruit & Milk teas", IsActive = true },
                    new Category { CategoryName = "Smoothies", Description = "Ice blended drinks", IsActive = true }
                };
                foreach(Category c in categories) {
                    context.Categories.Add(c);
                }
                context.SaveChanges();

                // Add drinks, variants, and toppings
                var coffeeCat = context.Categories.First(c => c.CategoryName == "Coffee").CategoryID;
                var drink = new Drink { DrinkName = "Black Coffee", CategoryID = coffeeCat, IsActive = true, CreatedAt = DateTime.Now };
                context.Drinks.Add(drink);
                context.SaveChanges();

                context.DrinkVariants.Add(new DrinkVariant { DrinkID = drink.DrinkID, SizeName = "M", Price = 30000, IsActive = true });
                context.DrinkVariants.Add(new DrinkVariant { DrinkID = drink.DrinkID, SizeName = "L", Price = 40000, IsActive = true });
                context.SaveChanges();
                
                context.Toppings.Add(new Topping { ToppingName = "Extra Espresso", ExtraPrice = 15000, IsActive = true });
                context.Toppings.Add(new Topping { ToppingName = "Caramel Syrup", ExtraPrice = 10000, IsActive = true });
                context.SaveChanges();
            }
        }

        private static void EnsureEmployeeEmailColumn(PolyCafeContext context)
        {
            context.Database.ExecuteSqlRaw("""
                IF COL_LENGTH('Employees', 'Email') IS NULL
                BEGIN
                    ALTER TABLE Employees ADD Email nvarchar(255) NOT NULL CONSTRAINT DF_Employees_Email DEFAULT '';
                END
                """);
        }
    }
}
