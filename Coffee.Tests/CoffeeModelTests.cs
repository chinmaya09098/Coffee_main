using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Coffee;

namespace Coffee.Tests
{
    [TestClass]
    public class CoffeeModelTests
    {
        // ─────────────────────────────────────────
        //  CONSTRUCTOR — Valid inputs
        // ─────────────────────────────────────────

        [TestMethod]
        public void Constructor_ValidInput_CreatesObject()
        {
            var cup = new CoffeeModel("Arabica", 2, true);

            Assert.AreEqual("Arabica", cup.BeansType);
            Assert.AreEqual(2,         cup.Sugar);
            Assert.AreEqual(true,      cup.WithMilk);
        }

        [TestMethod]
        public void Constructor_ZeroSugar_Succeeds()
        {
            var cup = new CoffeeModel("Robusta", 0, false);
            Assert.AreEqual(0, cup.Sugar);
        }

        [TestMethod]
        public void Constructor_MaxSugar_Succeeds()
        {
            var cup = new CoffeeModel("Robusta", 5, false);
            Assert.AreEqual(5, cup.Sugar);
        }

        [TestMethod]
        public void Constructor_WithoutMilk_Succeeds()
        {
            var cup = new CoffeeModel("Espresso", 1, false);
            Assert.AreEqual(false, cup.WithMilk);
        }

        // ─────────────────────────────────────────
        //  CONSTRUCTOR — Invalid inputs
        // ─────────────────────────────────────────

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_EmptyBeanType_ThrowsArgumentException()
        {
            new CoffeeModel("", 0, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WhitespaceBeanType_ThrowsArgumentException()
        {
            new CoffeeModel("   ", 0, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NullBeanType_ThrowsArgumentException()
        {
            new CoffeeModel(null, 0, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_SugarBelowZero_ThrowsArgumentException()
        {
            new CoffeeModel("Arabica", -1, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_SugarAboveFive_ThrowsArgumentException()
        {
            new CoffeeModel("Arabica", 6, false);
        }

        // ─────────────────────────────────────────
        //  ADD SUGAR
        // ─────────────────────────────────────────

        [TestMethod]
        public void AddSugar_ValidAmount_IncreasesSugar()
        {
            var cup = new CoffeeModel("Arabica", 0, false);
            cup.AddSugar(3);
            Assert.AreEqual(3, cup.Sugar);
        }

        [TestMethod]
        public void AddSugar_AddsToExisting_AccumulatesCorrectly()
        {
            var cup = new CoffeeModel("Arabica", 2, false);
            cup.AddSugar(2);
            Assert.AreEqual(4, cup.Sugar);
        }

        [TestMethod]
        public void AddSugar_ExactlyFive_Succeeds()
        {
            var cup = new CoffeeModel("Arabica", 0, false);
            cup.AddSugar(5);
            Assert.AreEqual(5, cup.Sugar);
        }

        [TestMethod]
        public void AddSugar_WouldExceedFive_DoesNotChange()
        {
            var cup = new CoffeeModel("Arabica", 3, false);
            cup.AddSugar(3); // 3 + 3 = 6 → exceeds max, should not change
            Assert.AreEqual(3, cup.Sugar);
        }

        [TestMethod]
        public void AddSugar_Zero_SugarUnchanged()
        {
            var cup = new CoffeeModel("Arabica", 2, false);
            cup.AddSugar(0);
            Assert.AreEqual(2, cup.Sugar);
        }

        [TestMethod]
        public void AddSugar_NegativeAmount_SugarUnchanged()
        {
            var cup = new CoffeeModel("Arabica", 2, false);
            cup.AddSugar(-1);
            Assert.AreEqual(2, cup.Sugar);
        }

        // ─────────────────────────────────────────
        //  DETAILS
        // ─────────────────────────────────────────

        [TestMethod]
        public void Details_WithMilk_ContainsWithMilk()
        {
            var cup = new CoffeeModel("Arabica", 2, true);
            Assert.IsTrue(cup.Details().Contains("With Milk"));
        }

        [TestMethod]
        public void Details_WithoutMilk_ContainsNoMilk()
        {
            var cup = new CoffeeModel("Arabica", 2, false);
            Assert.IsTrue(cup.Details().Contains("No Milk"));
        }

        [TestMethod]
        public void Details_ContainsBeanType()
        {
            var cup = new CoffeeModel("Arabica", 2, true);
            Assert.IsTrue(cup.Details().Contains("Arabica"));
        }

        [TestMethod]
        public void Details_ContainsSugarAmount()
        {
            var cup = new CoffeeModel("Arabica", 3, true);
            Assert.IsTrue(cup.Details().Contains("3"));
        }

        [TestMethod]
        public void Details_ReturnsNonEmptyString()
        {
            var cup = new CoffeeModel("Robusta", 1, false);
            Assert.IsFalse(string.IsNullOrEmpty(cup.Details()));
        }

        // ─────────────────────────────────────────
        //  PROPERTIES
        // ─────────────────────────────────────────

        [TestMethod]
        public void BeansType_ReturnsCorrectValue()
        {
            var cup = new CoffeeModel("Liberica", 0, false);
            Assert.AreEqual("Liberica", cup.BeansType);
        }

        [TestMethod]
        public void WithMilk_TrueReturnsTrue()
        {
            var cup = new CoffeeModel("Arabica", 0, true);
            Assert.IsTrue(cup.WithMilk);
        }

        [TestMethod]
        public void WithMilk_FalseReturnsFalse()
        {
            var cup = new CoffeeModel("Arabica", 0, false);
            Assert.IsFalse(cup.WithMilk);
        }

        [TestMethod]
        public void Sugar_ReturnsCorrectValue()
        {
            var cup = new CoffeeModel("Arabica", 4, false);
            Assert.AreEqual(4, cup.Sugar);
        }
    }
}
