namespace Base.Inventory.Item
{
    public interface IItem: IBasic<int>
    {
        public ItemType ItemType { get; }
        public int Value { get; }
        public int Amount();
        public void UpdateAmount(int amount);
        
        public string Identifier();

        public bool IsForGift();
    }

    public enum ItemType
    {
        Energy,         // Regular energy items (Milk, Energy Drink, Banh Mi, Lunch)
        SpecialEnergy,  // Special energy items (Rocket Drink - adds Stamina)
        Love,           // Love/gift items (Lipstick, Flower, Teddy Bear, Apron, Bikini, Gym Outfit, Sexy Sleepwear)
        Utility,        // Utility items (Condom - prevents pregnancy)
    }
}