using System;

namespace Coffee
{
    public class CoffeeModel
    {
        private string _beansType;
        private int _sugar;
        private bool _withMilk;

        public string BeansType => _beansType;
        public bool WithMilk => _withMilk;
        public int Sugar => _sugar;

        public CoffeeModel(string beansType, int sugar, bool withMilk)
        {
            if (string.IsNullOrWhiteSpace(beansType))
                throw new ArgumentException("Beans type cannot be empty.");

            if (sugar < 0 || sugar > 5)
                throw new ArgumentException("Sugar must be between 0 and 5.");

            _beansType = beansType;
            _sugar = sugar;
            _withMilk = withMilk;
        }

        public void AddSugar(int amount)
        {
            if (amount < 0)
                return;

            if (_sugar + amount > 5)
                return;

            _sugar += amount;
        }

        public string Details()
        {
            return string.Format("Bean: {0}  |  Sugar: {1}  |  {2}",
                _beansType, _sugar, _withMilk ? "With Milk" : "No Milk");
        }
    }
}
