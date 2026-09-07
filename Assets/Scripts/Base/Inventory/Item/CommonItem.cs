namespace Base.Inventory.Item
{
    public class CommonItem : IItem
    {
        public CommonItem(
            int price,
            string name,
            ItemType itemType,
            int itemValue,
            int amount = 0
        )
        {
            _fixedPrice = price;
            _amount = amount;
            _name = name;
            Value = itemValue;
            ItemType = itemType;
        }

        private int _amount;
        private readonly int _fixedPrice;
        private readonly string _name;

        public ItemType ItemType { get; }
        public int Value { get; }

        public int GetValues()
        {
            return _fixedPrice;
        }

        public int Amount()
        {
            return _amount;
        }

        public void UpdateAmount(int amount)
        {
            _amount = amount;
        }

        public string Identifier()
        {
            return _name;
        }

        public int IncreaseConnectValue()
        {
            return Value;
        }

        public bool IsForGift()
        {
            return ItemType == ItemType.Love;
        }
    }
}