# ☕ Coffee Order Console App

## Description
This is a simple console application written in **C#** that simulates a coffee ordering system.  

The program allows the user to:

- Select the **type of coffee beans**  
- Choose whether to add **milk**  
- Add **sugar** (with validation: 0-5 spoons)  
- Review and modify their coffee order  
- Place multiple orders in one run  

The project demonstrates **object-oriented programming (OOP)** concepts such as:

- **Encapsulation** (fields are private, controlled via methods/properties)  
- **Methods with validation** (`AddSugar`)  
- **Read-only properties** for immutable attributes  

---

## Features
- User-friendly console interface  
- Sugar validation (cannot exceed 5 spoons)  
- Dynamic multiple orders support  
- Clear output of coffee details  

---

## How to Run
1. Clone or download the repository  
2. Open the project in **Visual Studio**  
3. Run the program (`F5` or `Ctrl + F5`)  
4. Follow the prompts in the console to create coffee orders  

---

## Example
```
Enter Beans Type: Brazilian
Do you want milk? (y/n): y
Add sugar (0-5): 3
Current Cup: Bean Type: Brazilian, Sugar: 3, With Milk
Do you want to change sugar? (y/n): n
Do you want another order? (y/n): n
Thanks for your orders!
```

---

## Concepts Applied
- **Classes & Objects**: `Coffee` class encapsulates data and behavior  
- **Properties & Methods**: Controlled access to coffee attributes  
- **Validation & Error Handling**: Prevent invalid sugar amounts  
- **Loops**: Support for multiple orders  

---

## Future Improvements
- Add support for **sizes** (small, medium, large)  
- Include **extra flavors** (vanilla, caramel, etc.)  
- Implement **Immutable Coffee object** version for advanced OOP practice  
