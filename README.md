# Auto Insurance Management System

## Overview

The **Auto Insurance Management System** is a comprehensive web application designed to handle the end-to-end process of managing auto insurance policies, claims, payments, and customer support. Built with MVC architecture (supporting both Java Spring MVC and ASP.NET Core MVC), the system streamlines key insurance operations through modular development.

---

## Core Modules

1. **Policy Management**
   - Creation, modification, and retrieval of insurance policies.
2. **Claim Management**
   - Submission, review, and approval/rejection of policy claims.
3. **Payment Management**
   - Payment processing, history, and refunds for policies and claims.
4. **Customer Support**
   - Ticketing system for customer inquiries and issue resolution.
5. **User Authentication & Authorization**
   - Secure registration, login, and role-based access control (Admin, Agent, Customer).

---

## Module Structure

### 1. Policy Management Module

- **Endpoints:**  
  - `createPolicy()`, `updatePolicy()`, `getPolicyById()`, `getAllPolicies()`, `deletePolicy()`
- **Model Fields:**  
  - `policyId`, `policyNumber`, `vehicleDetails`, `coverageAmount`, `coverageType`, `premiumAmount`, `startDate`, `endDate`, `policyStatus` (ACTIVE, INACTIVE, RENEWED)

### 2. Claim Management Module

- **Endpoints:**  
  - `submitClaim()`, `getClaimDetails()`, `updateClaimStatus()`, `getAllClaims()`
- **Model Fields:**  
  - `claimId`, `policyId`, `claimAmount`, `claimDate`, `claimStatus` (OPEN, APPROVED, REJECTED), `adjusterId`

### 3. Payment Management Module

- **Endpoints:**  
  - `makePayment()`, `getPaymentDetails()`, `getPaymentsByPolicy()`
- **Model Fields:**  
  - `paymentId`, `paymentAmount`, `paymentDate`, `paymentStatus` (SUCCESS, FAILED, PENDING), `policyId`

### 4. Customer Support Module

- **Endpoints:**  
  - `createTicket()`, `getTicketDetails()`, `resolveTicket()`, `getAllTickets()`
- **Model Fields:**  
  - `ticketId`, `userId`, `issueDescription`, `ticketStatus` (OPEN, RESOLVED), `createdDate`, `resolvedDate`

### 5. User Authentication & Authorization Module

- **Endpoints:**  
  - `login()`, `registerUser()`, `logout()`, `getUserProfile()`
- **Model Fields:**  
  - `userId`, `username`, `password (hashed)`, `email`, `role` (ADMIN, AGENT, CUSTOMER)

---

## Database Schema

### 1. Policy Table

```sql
CREATE TABLE Policy (
  policyId INT AUTO_INCREMENT PRIMARY KEY,
  policyNumber VARCHAR(50) NOT NULL,
  vehicleDetails TEXT,
  coverageAmount DECIMAL(10, 2),
  coverageType VARCHAR(100),
  premiumAmount DECIMAL(10, 2),
  startDate DATE,
  endDate DATE,
  policyStatus ENUM('ACTIVE', 'INACTIVE', 'RENEWED')
);
```

### 2. Claim Table

```sql
CREATE TABLE Claim (
  claimId INT AUTO_INCREMENT PRIMARY KEY,
  policyId INT,
  claimAmount DECIMAL(10, 2),
  claimDate DATE,
  claimStatus ENUM('OPEN', 'APPROVED', 'REJECTED'),
  adjusterId INT,
  FOREIGN KEY (policyId) REFERENCES Policy(policyId),
  FOREIGN KEY (adjusterId) REFERENCES User(userId)
);
```

### 3. Payment Table

```sql
CREATE TABLE Payment (
  paymentId INT AUTO_INCREMENT PRIMARY KEY,
  policyId INT,
  paymentAmount DECIMAL(10, 2),
  paymentDate DATE,
  paymentStatus ENUM('SUCCESS', 'FAILED', 'PENDING'),
  FOREIGN KEY (policyId) REFERENCES Policy(policyId)
);
```

### 4. SupportTicket Table

```sql
CREATE TABLE SupportTicket (
  ticketId INT AUTO_INCREMENT PRIMARY KEY,
  userId INT,
  issueDescription TEXT,
  ticketStatus ENUM('OPEN', 'RESOLVED'),
  createdDate DATE,
  resolvedDate DATE,
  FOREIGN KEY (userId) REFERENCES User(userId)
);
```

### 5. User Table

```sql
CREATE TABLE User (
  userId INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) UNIQUE,
  password VARCHAR(255),
  email VARCHAR(100),
  role ENUM('ADMIN', 'AGENT', 'CUSTOMER')
);
```

---

## Assumptions

- Local deployment with MySQL or SQL Server as the database.
- Role-based access for Admin, Agent, Customer.
- Uses ORM tools: Hibernate (Java) or Entity Framework (.NET).
- Responsive and user-friendly UI.

---

## Local Deployment

### Prerequisites

- MySQL or SQL Server installed and configured.
- Required SDKs:
  - Java: JDK 17
  - .NET: .NET SDK 7.0
- Web server:
  - Java: Apache Tomcat
  - .NET: Kestrel

### Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/krishnana388/AutoInsuranceMS.git
   cd AutoInsuranceMS
   ```
2. **Set up the database:**  
   Execute the provided SQL schema in your MySQL or SQL Server instance.
3. **Configure application settings:**  
   Update the database connection details in `application.properties` (Java) or `appsettings.json` (.NET).
4. **Build and run the application:**  
   - Java: Deploy using Apache Tomcat  
   - .NET: Run with Kestrel (`dotnet run`)

---

## Conclusion

The Auto Insurance Management System offers a robust, modular, and scalable platform for managing all aspects of auto insurance operations—policy management, claims, payments, support, and user access—following industry best practices with clear separation of concerns and a clean database design.

---

## License

This project is licensed under the MIT License.

---

## Author

- [krishnana388](https://github.com/krishnana388)
