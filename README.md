# Shiv Furniture ERP

A comprehensive Enterprise Resource Planning (ERP) system built for furniture manufacturing and sales businesses. This system manages the complete business cycle from purchase orders to customer invoices, with integrated analytical accounting and budget management.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=flat)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=flat&logo=microsoft-sql-server)
![Razorpay](https://img.shields.io/badge/Razorpay-Payment%20Gateway-072654?style=flat)

---

## 📋 Table of Contents

- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Database Setup](#-database-setup)
- [Default Credentials](#-default-credentials)
- [Project Structure](#-project-structure)
- [Key Modules](#-key-modules)
- [Payment Integration](#-payment-integration)
- [Screenshots](#-screenshots)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

### Administrative Features
- 📊 **Dashboard Analytics** - Real-time business metrics and KPIs
- 👥 **Contact Management** - Comprehensive customer and vendor database
- 📦 **Product Catalog** - Product categorization with analytical account mapping
- 🛒 **Purchase Orders** - Complete procurement lifecycle management
- 📋 **Vendor Bills** - Bill creation, approval, and payment tracking
- 💰 **Bill Payments** - Multi-mode payment processing (Cash/Bank/Online)
- 📝 **Sales Orders** - Sales order creation and fulfillment
- 🧾 **Customer Invoices** - Automated invoice generation from sales orders
- 💳 **Online Payments** - Razorpay payment gateway integration
- 📊 **Analytical Accounting** - Multi-dimensional financial analysis
- 💼 **Budget Management** - Budget creation, tracking, and performance monitoring
- 🔄 **Auto-Analytical Models** - Automated account assignment rules
- 👤 **User Management** - Role-based access control (Admin/Portal users)
- 📧 **Email Notifications** - Automated email alerts and portal invitations
- 📄 **PDF Generation** - Professional invoice PDF generation

### Portal Features
- 🏠 **Customer Portal** - Self-service portal for customers
- 📑 **View Invoices** - Access all invoices and payment history
- 💳 **Pay Online** - Secure online payment via Razorpay
- 📥 **Download Invoices** - PDF invoice downloads
- 🔐 **Secure Authentication** - Password policies and forced password changes
- 📊 **Financial Dashboard** - View account balances and transactions

### Security Features
- 🔒 **ASP.NET Core Identity** - Robust authentication system
- 👮 **Role-Based Authorization** - Admin and Portal user policies
- 🔑 **Password Enforcement** - Configurable password complexity rules
- ⏱️ **Session Management** - Role-specific session timeouts
- 🚪 **Forced Password Changes** - Security for new portal accounts

---

## 🛠 Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0 MVC
- **Language**: C# 12.0
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server

### Frontend
- **View Engine**: Razor Pages
- **CSS Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons
- **JavaScript**: Vanilla JS + Razorpay Checkout

### Payment Integration
- **Gateway**: Razorpay
- **Package**: Razorpay .NET SDK

### PDF Generation
- **Library**: QuestPDF 2025.12.3

### Email
- **SMTP**: Gmail SMTP with app passwords

---

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2019+](https://www.microsoft.com/sql-server/sql-server-downloads) or SQL Server Express
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) (optional)

---

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/ShivFurnitureERP.git
cd ShivFurnitureERP
```

### 2. Restore Dependencies

```bash
cd ShivFurnitureERP
dotnet restore
```

### 3. Configure Connection String

Edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=ShivFurnitureERP;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

**Connection String Options:**

**Windows Authentication:**
```
Server=localhost\\SQLEXPRESS;Database=ShivFurnitureERP;Integrated Security=True;TrustServerCertificate=True;
```

**SQL Server Authentication:**
```
Server=localhost\\SQLEXPRESS;Database=ShivFurnitureERP;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

### 4. Run Migrations

The application automatically runs migrations on startup. Alternatively, run manually:

```bash
dotnet ef database update
```

### 5. Build and Run

```bash
dotnet build
dotnet run
```

The application will start at `https://localhost:5001`

---

## ⚙️ Configuration

### Email Configuration

Configure SMTP settings in `appsettings.json`:

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "UserName": "your-email@gmail.com",
    "Password": "your-app-password",
    "From": "your-email@gmail.com",
    "BaseUrl": "https://localhost:5001"
  }
}
```

**Gmail App Password Setup:**
1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable 2-Step Verification
3. Generate an App Password
4. Use the generated password in `appsettings.json`

### Razorpay Configuration

Configure Razorpay credentials in `appsettings.json`:

```json
{
  "Razorpay": {
    "KeyId": "rzp_test_YOUR_KEY_ID",
    "KeySecret": "YOUR_KEY_SECRET",
    "CompanyName": "Shiv Furniture ERP",
    "CompanyLogo": "",
    "ThemeColor": "#3399cc"
  }
}
```

**Get Razorpay Credentials:**
1. Sign up at [Razorpay Dashboard](https://dashboard.razorpay.com/)
2. Navigate to Settings → API Keys
3. Generate Test or Live keys
4. Copy `Key ID` and `Key Secret` to `appsettings.json`

**Test Mode:**
- Use keys starting with `rzp_test_`
- Test cards: 4111 1111 1111 1111
- Any future expiry date and CVV

**Production:**
- Replace with live keys: `rzp_live_`
- Complete KYC verification
- Enable webhooks (optional)

---

## 🗄️ Database Setup

### Automatic Setup

The application automatically:
1. Creates the database on first run
2. Applies all migrations
3. Seeds default data (roles, admin user, sample data)

### Manual Setup

If needed, run migrations manually:

```bash
# Create migration
dotnet ef migrations add YourMigrationName

# Apply migrations
dotnet ef database update

# Revert migration
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove
```

### Seed Data

The following data is automatically seeded:

**Roles:**
- Admin
- PortalUser

**Default Admin User:**
- Login ID: `ADMIN001`
- Password: `Admin@123`
- Email: `admin@shivfurniture.com`

**Sample Data:**
- Products (e.g., Sofa, Table, Chair)
- Analytical Accounts (Revenue, Expenses, Assets)
- Product Categories

---

## 🔐 Default Credentials

### Admin Area
**URL:** `https://localhost:5001/Admin/Account/Login`

| Login ID | Password | Role |
|----------|----------|------|
| ADMIN001 | Admin@123 | Admin |

### Portal Area
Portal users are created by admins when creating contacts (customers/vendors). Each contact receives:
- Auto-generated Login ID (e.g., `JOHN123456`)
- Temporary password via email
- Forced password change on first login

**Test Portal User (if seeded):**
- Check database or create a contact via Admin panel

---

## 📁 Project Structure

```
ShivFurnitureERP/
├── Areas/
│   ├── Admin/                    # Admin area (back-office)
│   │   ├── Controllers/          # Admin controllers
│   │   └── Views/                # Admin views
│   └── Portal/                   # Customer portal (front-office)
│       ├── Controllers/          # Portal controllers
│       └── Views/                # Portal views
├── Controllers/                  # Root controllers
├── Data/                         # DbContext
├── Infrastructure/               # Seeding, conventions
├── Middleware/                   # Custom middleware
├── Migrations/                   # EF Core migrations
├── Models/                       # Domain entities
├── Options/                      # Configuration classes
├── Repositories/                 # Data access layer
├── Services/                     # Business logic
├── ViewModels/                   # View models
├── Views/                        # Shared views
├── wwwroot/                      # Static files (CSS, JS, images)
│   ├── css/                      # Stylesheets
│   ├── js/                       # JavaScript
│   └── uploads/                  # User uploads
├── appsettings.json              # App configuration
├── Program.cs                    # Application entry point
└── README.md                     # This file
```

---

## 🔧 Key Modules

### 1. Contact Management
- Create customers and vendors
- Tag-based organization
- Automatic portal account creation
- Email invitations with credentials

### 2. Purchase Orders
- Create POs from vendor contacts
- Multi-line items with analytical accounts
- Budget warning system
- PO approval workflow (Draft → Confirmed → Cancelled)

### 3. Vendor Bills
- Generate bills from POs
- Multi-payment support (Cash/Bank/Cheque)
- Payment tracking
- Bill status management

### 4. Sales Orders
- Customer order management
- Product-wise line items
- Analytical account assignment
- Order status workflow

### 5. Customer Invoices
- Auto-generate from sales orders
- Payment recording (Cash/Bank/Online)
- Razorpay integration for online payments
- PDF invoice generation
- Payment status tracking

### 6. Analytical Accounting
- Multi-dimensional account structure
- Budget creation and tracking
- Performance monitoring (Revenue/Expenses/Balance)
- Budget revisions and history
- Transaction-level analytical data

### 7. Auto-Analytical Models
- Rule-based automatic account assignment
- Product-category mapping
- Vendor/Customer-based rules
- Reduces manual data entry

---

## 💳 Payment Integration

### Razorpay Features

✅ **Secure Payment Processing**
- HMAC SHA256 signature verification
- Order creation and payment capture
- Support for partial payments
- Real-time payment status

✅ **Payment Methods Supported**
- Credit/Debit Cards
- Net Banking
- UPI
- Wallets

✅ **Admin & Portal Integration**
- **Admin Area:** Record payments for customer invoices
- **Portal Area:** Customers pay invoices online
- **Fallback:** Manual payment recording (Cash/Bank)

### Payment Flow

1. **Invoice Created** → Invoice in Draft status
2. **Invoice Confirmed** → Available for payment
3. **Customer/Admin Initiates Payment** → Razorpay order created
4. **Payment Completed** → Signature verified
5. **Payment Recorded** → Invoice status updated

### Test Razorpay

**Test Card Details:**
- Card Number: `4111 1111 1111 1111`
- Expiry: Any future date
- CVV: Any 3 digits
- Name: Any name

**Test UPI:**
- UPI ID: `success@razorpay` (success)
- UPI ID: `failure@razorpay` (failure)

---

## 📸 Screenshots

### Admin Dashboard
![Admin Dashboard](docs/screenshots/admin-dashboard.png)

### Customer Invoice with Razorpay
![Invoice Payment](docs/screenshots/invoice-payment.png)

### Analytical Budget
![Budget Management](docs/screenshots/budget-management.png)

### Portal Dashboard
![Portal Dashboard](docs/screenshots/portal-dashboard.png)

---

## 🐛 Troubleshooting

### Database Connection Issues

**Error:** `Cannot connect to database`

**Solution:**
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Test connection via SSMS
4. Ensure `TrustServerCertificate=True` is set

### Migration Errors

**Error:** `Unable to create migration`

**Solution:**
```bash
# Remove last migration
dotnet ef migrations remove

# Clean and rebuild
dotnet clean
dotnet build

# Create migration again
dotnet ef migrations add YourMigrationName
```

### Email Not Sending

**Error:** SMTP authentication failed

**Solution:**
1. Use Gmail App Password (not account password)
2. Enable "Less secure app access" (if using old method)
3. Check SMTP settings in `appsettings.json`
4. Verify port 587 is open

### Razorpay Issues

**Error:** Payment signature verification failed

**Solution:**
1. Verify `KeyId` and `KeySecret` match
2. Check Razorpay dashboard for test/live mode
3. Clear browser cache
4. Check browser console for errors

**Error:** Checkout not opening

**Solution:**
1. Ensure Razorpay script is loaded: `https://checkout.razorpay.com/v1/checkout.js`
2. Check browser console for errors
3. Verify `KeyId` is correct

### Port Already in Use

**Error:** `Address already in use`

**Solution:**
```bash
# Change port in Properties/launchSettings.json
# Or kill process using port 5001
netstat -ano | findstr :5001
taskkill /PID <process_id> /F
```

### Hot Reload Errors

**Solution:**
1. Stop debugging
2. Clean solution: `dotnet clean`
3. Rebuild: `dotnet build`
4. Restart application

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards

- Follow C# naming conventions
- Use meaningful variable names
- Add XML documentation for public methods
- Write unit tests for business logic
- Update README for new features

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📧 Contact & Support

**Developer:** Shiv Furniture ERP Team  
**Email:** support@shivfurniture.com  
**Website:** [https://shivfurniture.com](https://shivfurniture.com)

### Report Issues

Found a bug? Have a feature request?  
[Create an issue](https://github.com/yourusername/ShivFurnitureERP/issues)

---

## 🙏 Acknowledgments

- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) - Web framework
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM
- [Bootstrap](https://getbootstrap.com/) - UI framework
- [Razorpay](https://razorpay.com/) - Payment gateway
- [QuestPDF](https://www.questpdf.com/) - PDF generation
- [Bootstrap Icons](https://icons.getbootstrap.com/) - Icon library

---

## 🗺️ Roadmap

### Upcoming Features

- [ ] Multi-currency support
- [ ] Inventory management
- [ ] Production planning
- [ ] Advanced reporting
- [ ] Mobile app
- [ ] API integration
- [ ] WhatsApp notifications
- [ ] Multi-language support
- [ ] Advanced analytics
- [ ] Warehouse management

---

## 📊 System Requirements

### Minimum Requirements
- **OS:** Windows 10/11, macOS, Linux
- **RAM:** 4 GB
- **Storage:** 2 GB
- **Database:** SQL Server 2019+

### Recommended Requirements
- **OS:** Windows Server 2022, Ubuntu 22.04
- **RAM:** 8 GB
- **Storage:** 10 GB SSD
- **Database:** SQL Server 2022

---

## 🌐 Browser Support

| Browser | Version |
|---------|---------|
| Chrome | Latest |
| Firefox | Latest |
| Safari | Latest |
| Edge | Latest |

---

**Built with ❤️ by Shiv Furniture ERP Team**
