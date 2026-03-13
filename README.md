# 🚀 VentureMatch – Co-Founder Finder Platform

---

# 📌 Project Description

VentureMatch is a full-stack web application that helps entrepreneurs find startup co-founders based on their skills, interests, and startup ideas.

Many founders have great ideas but lack the right partners to build them. VentureMatch solves this by allowing users to discover potential co-founders, match with them, collaborate on ideas, and communicate in real time.

This project demonstrates modern full-stack development using Angular, ASP.NET Core Web API, MongoDB, and real-time communication with SignalR.

---

# 🚀 Features

## ✅ Implemented Features

✔ User registration and login  
✔ Secure authentication using JWT  
✔ User profile creation and management  
✔ Co-founder discovery system  
✔ Like / Pass potential co-founders  
✔ Mutual match detection  
✔ Real-time messaging using SignalR  
✔ Startup idea posting and collaboration  
✔ Apply to join startup teams  

---

# 🛠 Tech Stack

## 🖥 Frontend

- Angular  
- TypeScript  
- Bootstrap  
- RxJS  

## ⚙️ Backend

- ASP.NET Core Web API  
- .NET  
- Swagger / OpenAPI  
- SignalR  

## 🗄 Database

- MongoDB Atlas  
- MongoDB Compass  

## 🔧 Tools & Technologies

- JWT Authentication  
- REST APIs  
- Swagger API Documentation  
- Git & GitHub  

---

# 📂 Project Structure

```
VentureMatch
│
├── backend
│   │
│   ├── Controllers
│   │   ├── AuthController.cs
│   │   ├── UserController.cs
│   │   ├── MatchController.cs
│   │   ├── MessageController.cs
│   │   └── StartupIdeaController.cs
│   │
│   ├── Models
│   │   ├── User.cs
│   │   ├── Match.cs
│   │   ├── Message.cs
│   │   └── StartupIdea.cs
│   │
│   ├── DTOs
│   │   ├── LoginDTO.cs
│   │   └── RegisterDTO.cs
│   │
│   ├── Services
│   │   ├── AuthService.cs
│   │   ├── MatchService.cs
│   │   └── MessageService.cs
│   │
│   ├── Data
│   │   └── MongoDbContext.cs
│   │
│   └── Program.cs
│
├── frontend
│   │
│   ├── src
│   │   │
│   │   ├── app
│   │   │   │
│   │   │   ├── components
│   │   │   │   ├── login
│   │   │   │   ├── signup
│   │   │   │   ├── dashboard
│   │   │   │   ├── matches
│   │   │   │   ├── chat
│   │   │   │   └── startup-ideas
│   │   │   │
│   │   │   ├── services
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── match.service.ts
│   │   │   │   └── message.service.ts
│   │   │   │
│   │   │   └── models
│   │   │       ├── user.model.ts
│   │   │       └── message.model.ts
│   │   │
│   │   ├── assets
│   │   │
│   │   └── environments
│   │
│   └── package.json
│
├── screenshots
│   ├── loginpage.png
│   ├── signuppage.png
│   ├── dashboardpage.png
│   ├── matchespage.png
│   ├── messagepage.png
│   └── startupidea.png
│
└── README.md
```

---

## Demo / Screenshots

### 🔐 Login Page
![Login Page](screenshots/loginpage.png)

### 📝 Signup Page
![Signup Page](screenshots/signuppage.png)

### 📊 Dashboard
![Dashboard](screenshots/dashboardpage.png)

### 🤝 Co-Founder Matching
![Matching](screenshots/matchespage.png)

### 💬 Messages / Chat
![Messages](screenshots/messagepage.png)

### 💡 Startup Ideas
![Startup Ideas](screenshots/startupidea.png)


---

# ⚙️ Installation

## 1️⃣ Clone the Repository

```
git clone https://github.com/DurgaSrujana57/VentureMatch.git
cd VentureMatch
```

---

## 2️⃣ Install Frontend Dependencies

```
cd frontend
npm install
```

---

## 3️⃣ Restore Backend Packages

```
cd ../backend
dotnet restore
```

---

# ▶️ Run the Project

## Backend

```
cd backend
dotnet run
```

Backend runs at:

```
http://localhost:5000
```

Swagger API Documentation:

```
http://localhost:5000/swagger
```

---

## Frontend

```
cd frontend
ng serve
```

Frontend runs at:

```
http://localhost:4200
```

---

# 🔮 Future Improvements

- AI-based co-founder recommendations  
- Email notifications  
- Push notifications  
- Mobile application version  
- Advanced team collaboration tools  

---

# 👩‍💻 Author

Durga Srujana  

GitHub  
https://github.com/DurgaSrujana57

---

# 📄 License

This project is licensed under the **MIT License**.
