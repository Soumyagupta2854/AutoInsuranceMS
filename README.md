# 🚗 Auto Insurance Management System

A **comprehensive, modular web application** for managing auto insurance policies, claims, payments, and customer support. Designed for scalability and maintainability, this system leverages MVC architecture (Java Spring MVC or ASP.NET Core MVC) and a robust database schema to streamline key insurance business operations.

---

## ✨ Features at a Glance

- **Policy Lifecycle Management:** Effortless creation, modification, renewal, and tracking of insurance policies.
- **Claims Processing:** Seamless submission, review, and adjudication of insurance claims.
- **Integrated Payments:** Secure management of payments, refunds, and payment history.
- **Customer Support Portal:** Built-in ticketing system for prompt issue resolution.
- **Enterprise-Grade Security:** Role-based access (Admin, Agent, Customer) with secure authentication and authorization.

---

## 🏗️ Core Modules & APIs

### 1. Policy Management

| Action          | Endpoint           | Key Fields                                                    |
|-----------------|--------------------|---------------------------------------------------------------|
| Create Policy   | `createPolicy()`   | `policyId`, `policyNumber`, `vehicleDetails`, `coverageAmount`, `premiumAmount`, `startDate`, `endDate`, `policyStatus` (ACTIVE, INACTIVE, RENEWED) |
| Update Policy   | `updatePolicy()`   | ...                                                           |
| Get Policy      | `getPolicyById()`  | ...                                                           |
| List Policies   | `getAllPolicies()` | ...                                                           |
| Delete Policy   | `deletePolicy()`   | ...                                                           |

### 2. Claim Management

| Action             | Endpoint                 | Key Fields                                                  |
|--------------------|--------------------------|-------------------------------------------------------------|
| Submit Claim       | `submitClaim()`          | `claimId`, `policyId`, `claimAmount`, `claimDate`, `claimStatus` (OPEN, APPROVED, REJECTED), `adjusterId` |
| Get Claim Details  | `getClaimDetails()`      | ...                                                         |
| Update Claim Status| `updateClaimStatus()`    | ...                                                         |
| List Claims        | `getAllClaims()`         | ...                                                         |

### 3. Payment Management

| Action               | Endpoint                  | Key Fields                                    |
|----------------------|---------------------------|-----------------------------------------------|
| Make Payment         | `makePayment()`           | `paymentId`, `paymentAmount`, `paymentDate`, `paymentStatus` (SUCCESS, FAILED, PENDING), `policyId` |
| Get Payment Details  | `getPaymentDetails()`     | ...                                           |
| Payments by Policy   | `getPaymentsByPolicy()`   | ...                                           |

### 4. Customer Support

| Action              | Endpoint                 | Key Fields                                           |
|---------------------|--------------------------|------------------------------------------------------|
| Create Ticket       | `createTicket()`         | `ticketId`, `userId`, `issueDescription`, `ticketStatus` (OPEN, RESOLVED), `createdDate`, `resolvedDate` |
| Get Ticket Details  | `getTicketDetails()`     | ...                                                  |
| Resolve Ticket      | `resolveTicket()`        | ...                                                  |
| List All Tickets    | `getAllTickets()`        | ...                                                  |

### 5. User Authentication & Authorization

| Action            | Endpoint               | Key Fields                                 |
|-------------------|------------------------|--------------------------------------------|
| Register          | `registerUser()`        | `userId`, `username`, `password (hashed)`, `email`, `role` (ADMIN, AGENT, CUSTOMER) |
| Login             | `login()`               | ...                                        |
| Logout            | `logout()`              | ...                                        |
| Get Profile       | `getUserProfile()`      | ...                                        |

---

## 🗄️ Database Schema Overview

- **Policy**: Policies and coverage details  
- **Claim**: Claims linked to policies & users  
- **Payment**: Tracks all payments  
- **SupportTicket**: Customer support requests  
- **User**: All users and roles  

<details>
<summary>View SQL Table Definitions</summary>

```sql
-- Policy Table
CREATE TABLE Policy (
  policyId INT AUTO_INCREMENT PRIMARY KEY,
  policyNumber VARCHAR(50) NOT NULL,
  vehicleDetails TEXT,
  coverageAmount DECIMAL(10,2),
  coverageType VARCHAR(100),
  premiumAmount DECIMAL(10,2),
  startDate DATE,
  endDate DATE,
  policyStatus ENUM('ACTIVE', 'INACTIVE', 'RENEWED')
);

-- Claim Table
CREATE TABLE Claim (
  claimId INT AUTO_INCREMENT PRIMARY KEY,
  policyId INT,
  claimAmount DECIMAL(10,2),
  claimDate DATE,
  claimStatus ENUM('OPEN', 'APPROVED', 'REJECTED'),
  adjusterId INT,
  FOREIGN KEY (policyId) REFERENCES Policy(policyId),
  FOREIGN KEY (adjusterId) REFERENCES User(userId)
);

-- Payment Table
CREATE TABLE Payment (
  paymentId INT AUTO_INCREMENT PRIMARY KEY,
  policyId INT,
  paymentAmount DECIMAL(10,2),
  paymentDate DATE,
  paymentStatus ENUM('SUCCESS', 'FAILED', 'PENDING'),
  FOREIGN KEY (policyId) REFERENCES Policy(policyId)
);

-- SupportTicket Table
CREATE TABLE SupportTicket (
  ticketId INT AUTO_INCREMENT PRIMARY KEY,
  userId INT,
  issueDescription TEXT,
  ticketStatus ENUM('OPEN', 'RESOLVED'),
  createdDate DATE,
  resolvedDate DATE,
  FOREIGN KEY (userId) REFERENCES User(userId)
);

-- User Table
CREATE TABLE User (
  userId INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) UNIQUE,
  password VARCHAR(255),
  email VARCHAR(100),
  role ENUM('ADMIN', 'AGENT', 'CUSTOMER')
);
```
</details>

---

## 🚀 Quick Start

### Prerequisites

- **Database:** MySQL or SQL Server
- **SDKs:**  
  - Java: JDK 17  
  - .NET: .NET SDK 7.0
- **Web Server:**  
  - Java: Apache Tomcat  
  - .NET: Kestrel

### 1. Clone the Repository

```bash
git clone https://github.com/krishnana388/AutoInsuranceMS.git
cd AutoInsuranceMS
```

### 2. Set Up the Database

Run the provided SQL scripts on your MySQL or SQL Server instance.

### 3. Configure Application

Update your database connection in:
- `application.properties` (Java)
- `appsettings.json` (.NET)

### 4. Build & Deploy

- **Java:** Deploy to Apache Tomcat
- **.NET:** Run with Kestrel

---

## 💡 Highlights & Best Practices

- **Clean MVC Separation:** Ensures maintainability and testability.
- **ORM Support:** Hibernate (Java) or Entity Framework (.NET).
- **Responsive Design:** UI adapts to desktops, tablets, and mobiles.
- **Role-Based Security:** Fine-grained access for Admin, Agent, and Customer.
- **Scalable & Modular:** Easily extend modules or integrate new features.

---

## 📄 License

Distributed under the MIT License. See [LICENSE](LICENSE) for details.

---

## 👤 Author

- [krishnana388](https://github.com/krishnana388)

---

> _Empowering insurers and customers with efficient, secure, and scalable auto insurance management._
