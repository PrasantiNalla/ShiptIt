//Franc said this should not be needed as this is all done by the psql command


// using ShipIt.Models.DataModels;
// using Microsoft.EntityFrameworkCore;

// namespace ShipIt
// {
//     public class ShipItContext : DbContext
//     {
//         // Put all the tables you want in your database here
//         public DbSet<CompanyDataModel> Companies { get; set; }

//         public DbSet<EmployeeDataModel> Employee {get;set;}
        
//         public DbSet <StockDataModel> Stock {get;set;}
                  
//         public DbSet <DatabaseColumnName> Product {get;set;}


//         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//         { 
//             // This is the configuration used for connecting to the database
//             optionsBuilder.UseNpgsql(@"Server=localhost;Port=5432;Database=bookish;User Id=bookish;Password=bookish;");
//         }
//     }
// }