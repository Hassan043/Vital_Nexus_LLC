# VitalNexus - Educational Wellness Platform

An educational wellness application that helps users understand their lab results through kid-friendly explanations, personalized wellness guidance, and actionable insights.

## ⚠️ IMPORTANT DISCLAIMER

This application is for **EDUCATIONAL PURPOSES ONLY**. It does NOT provide medical advice, diagnosis, or treatment. Always consult qualified healthcare professionals before making health decisions.

## 🎯 Features

- **Lab Report Management**: Enter lab values manually with privacy-first design
- **Educational Explanations**: 5th-grade reading level explanations of lab markers
- **Food Recommendations**: Evidence-based food sources for each marker
- **Export Reports**: Generate Excel and PDF reports
- **Pet Support**: Track lab results for dogs and cats
- **Focus Areas**: Automatically identify top priority markers
- **Secure Authentication**: JWT-based auth with password reset flow

## 📋 Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **macOS or Linux** - This project uses bash scripts

## 🚀 Quick Start

### 1. Clone and Navigate
```bash
cd VitalNexus
```

### 2. Install Dependencies
```bash
# Backend
cd backend
dotnet restore

# Frontend
cd ../frontend
npm install
```

### 3. Configure Environment Variables (Optional for Email)

Create a `.env` file or configure `appsettings.json` for SMTP:
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "your-email@gmail.com",
    "Pass": "your-app-password",
    "From": "noreply@vitalnexus.com"
  }
}
```

**Note**: If SMTP is not configured, password reset links will be logged to console for local testing.

### 4. Run the Application

**Backend** (from `backend/` directory):
```bash
dotnet run --urls "http://localhost:5000"
```

**Frontend** (from `frontend/` directory):
```bash
npm run dev
```

### 5. Access Application
- **Frontend**: http://localhost:5173
- **Backend API**: http://localhost:5000
- **API Documentation**: http://localhost:5000/swagger

## 🔐 Authentication Features

### Register
- Navigate to `/register`
- Email + password + confirm password
- Auto-login after registration

### Login
- Navigate to `/login`
- Email + password
- Show/hide password toggle

### Forgot Password
- Click "Forgot password?" on login page
- Enter email
- Receive reset link (email or console if SMTP not configured)
- Link expires in 60 minutes

### Reset Password
- Click link from email
- Enter new password + confirm
- Redirects to login on success

## 🗄️ Database

Uses **SQLite** for local development. The database file (`nutrient.db`) is created automatically on first run.

### Tables
- Users
- LabReports (with `reportPublicId` for privacy)
- LabMarkers
- PetProfiles
- PasswordResetTokens
- BodyTendencyProfiles
- ExercisePlans

## Database Schema Management
The `/database` directory contains SQL Database Projects that are the authoritative source of truth for Azure SQL schema.

- `/database` contains the SQL Database Projects for account business, patient health, and function operations.
- SQL Database Projects are the source of truth for schema and object deployment.
- Deployment uses `DACPAC` artifacts and `sqlpackage` for Azure SQL publishing.
- EF Core in `backend/` is used for runtime data access only.
- EF Core migrations are not the production schema deployment mechanism.
- `/data` is reserved for static/reference data files such as `markers.json`.

## 📊 Supported Lab Markers

### CBC
- White Blood Cell Count (WBC)
- Red Blood Cell Count (RBC)
- Hemoglobin
- Hematocrit
- Platelets

### Metabolic Panel
- Glucose (Fasting)
- Sodium, Potassium, Chloride
- CO2 (Bicarbonate)
- BUN, Creatinine, Calcium
- Fasting Insulin

### Lipid Panel
- Total Cholesterol
- LDL Cholesterol
- HDL Cholesterol
- Triglycerides

### Thyroid
- TSH, Free T4, Free T3

### Vitamins & Minerals
- Vitamin D (25-OH)
- Vitamin B12
- Magnesium

### Iron Panel
- Ferritin
- Serum Iron
- TIBC
- Transferrin Saturation

### Liver
- ALT, AST, ALP
- Bilirubin (Total)
- Total Protein, Albumin, Globulin

### Inflammation
- CRP, hs-CRP

## 🔒 Security & Privacy

### Privacy-First Design
- No storage of full name, DOB, address, phone, or MRN
- Email only used for authentication
- Reports identified by `reportPublicId` (e.g., VN-20250215-A3X9K2)
- No email in exports or UI display
- User-specific data isolation via UUID

### Authentication Security
- BCrypt password hashing (cost factor 10)
- JWT with HS256 signing (24-hour expiration)
- Password reset tokens use SHA-256 hashing
- Single-use reset tokens with 60-minute expiration
- Rate limiting considerations for forgot password

### Logging
- Authorization headers redacted in logs
- No request body logging for marker values
- No PHI in application logs

## 📤 Export Formats

- **Excel (.xlsx)**: Multi-tab report with tracking templates
- **PDF/HTML**: Complete wellness report with educational content

Exports use `reportPublicId` for filenames, never email or personal info.

## 🧪 Testing Password Reset Locally

If SMTP is not configured:

1. Submit forgot password request
2. Check backend console logs for reset link
3. Copy link and paste in browser
4. Set new password

Example log output:
```
SMTP not configured. Reset link: http://localhost:5173/reset-password?token=ABC123...
```

## 🌐 Environment Variables

## Database Schema Management

The `/database` folder contains SQL Database Projects that define the Azure SQL schema and serve as the source of truth for database objects and deployment.

- `VitalNexus.AccountBusiness.Database` - database project for account and business data
- `VitalNexus.PatientHealth.Database` - database project for patient health data
- `VitalNexus.FunctionOperations.Database` - database project for functions and operational objects
- `VitalNexus.Database.Shared` - shared scripts, naming conventions, and deployment rules

### Shared SQL Conventions and Naming Standards
The `/database/VitalNexus.Database.Shared` project contains the authoritative shared conventions for Azure SQL database objects.
- `NamingConventions.md` describes naming rules for tables, columns, keys, indexes, procedures, and functions.
- `DeploymentRules.md` defines how to organize deployment scripts and maintain project-level consistency.
- Use these shared conventions across all SQL Database Projects to ensure consistent object names, schema organization, and deployment behavior.

Deployment is performed using DACPAC artifacts and `sqlpackage`/`SqlPackage.exe`.
EF Core in `backend/` is used for runtime data access only and is not the production schema deployment mechanism. Keep schema changes in the SQL Database Projects and deploy via DACPAC.

The `/data` folder is reserved for static/reference JSON files (for example `markers.json`) and does not contain schema files.


### Required for Production
```bash
# SMTP Configuration
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASS=your-app-password
SMTP_FROM=noreply@vitalnexus.com

# Frontend URL (for email links)
FRONTEND_BASE_URL=https://your-domain.com
```

### Optional
```bash
# JWT Configuration (use strong secret in production)
JWT_KEY=your-secret-key-here
```

## 🐛 Known Limitations

- PDF export is HTML format (needs proper PDF library for production)
- No CSV import (manual entry only)
- No exercise plan generation (stub model exists)
- No body tendency assessment (stub model exists)
- Basic SMTP email (consider transactional email service for production)

## 📝 License

Educational use only. Not for commercial distribution.

---

**Remember**: This tool is educational only. Always consult healthcare professionals for medical advice.