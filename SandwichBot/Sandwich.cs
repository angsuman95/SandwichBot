using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 649

namespace SandwichBot
{
    public enum SandwichOptions
    {
        BLT, BlackForestHam, BuffaloChicken, ChickenAndBaconRanchMelt, ColdCutCombo, MeatballMarinara, OverRoastedChicken, RoastBeef,
        [Terms(@"rotis\w* style chicken", MaxPhrase = 3)]
        RotisserieStyleChicken, SweetOnionTeriyaki, Tuna, TurkeyBreast, Veggie
    };

    public enum LengthOptions
    {
        SixInch, FootLong
    };

    public enum BreadOptions
    {
        NineGrainWheat, NineGrainHoneyOat, Italian, ItalianHerbsAndCheese, Flatbread
    };

    public enum CheeseOptions
    {
        American, MontereyCeddar, Pepperjack
    };

    public enum ToppingOptions
    {
        [Terms("encept", "but", "not", "no", "all", "everything")]
        Everything = 1,
        Avocado, BananaPeppers, Cucumbers, GreenBellPeppers, Jalapenos, Lettuce, Olives, Pickles, RedOnions, Spinach, Tomatoes
    };

    public enum SauceOptions
    {
        ChipotleSouthwest, HoneyMustard, LightMayonnaise, RegularMayonnaise, Mustard, Oil, Pepper, Ranch, SweetOnion, Vinegar
    };

    [Serializable]
    [Template(TemplateUsage.NotUnderstood, "I do not understand \"{0}\".", "Try again, I don't get \"{0}\".")]
    [Template(TemplateUsage.EnumSelectOne, "What kind of {&} would like on your sandwich? {||}", ChoiceStyle = ChoiceStyleOptions.PerLine)]
    public class SandwichOrder
    {
        [Prompt("What kind of {&} would you like? {||}")]
        public SandwichOptions? Sandwich;

        [Prompt("What size of sandwich do you want? {||}")]
        public LengthOptions? Length;

        public BreadOptions? Bread;

        [Optional]
        public CheeseOptions? Cheese;

        [Optional]
        public List<ToppingOptions> Toppings
        {
            get { return _toppings; }
            set
            {
                if (value != null && value.Contains(ToppingOptions.Everything))
                {
                    _toppings = (from ToppingOptions topping in Enum.GetValues(typeof(ToppingOptions))
                                 where topping != ToppingOptions.Everything && !value.Contains(topping)
                                 select topping).ToList();
                }
                else
                {
                    _toppings = value;
                }
            }
        }
        private List<ToppingOptions> _toppings;

        [Optional]
        public List<SauceOptions> Sauces;

        [Optional]
        [Template(TemplateUsage.NoPreference, "None")]
        public string Specials;

        public string DeliveryAddress;

        [Optional]
        public DateTime? DeliveryTime;

        [Numeric(1, 5)]
        [Optional]
        public double? Rating;

        public static IForm<SandwichOrder> BuildForm()
        {
            OnCompletionAsyncDelegate<SandwichOrder> processOrder = async (context, state) =>
            {
                await context.PostAsync("We are currently processing your sandwich. We will message you the status.");
            };

            return new FormBuilder<SandwichOrder>()
                .Message("Welcome to sandwich order bot!")
                .Field(nameof(Sandwich))
                .Field(nameof(Length))
                .Field(nameof(Bread))
                .Field(nameof(Cheese))
                .Field(nameof(Toppings))
                .Message("For sandwich topping you have selected {Toppings}.")
                .Field(nameof(Sauces))
                .Field(new FieldReflector<SandwichOrder>(nameof(Specials))
                    .SetType(null)
                    .SetActive((state) => state.Length == LengthOptions.FootLong)
                    .SetDefine(async (state, field) =>
                    {
                        field
                            .AddDescription("cookie", "Free Cookie")
                            .AddTerms("cookie", "cookie", "free cookie")
                            .AddDescription("drink", "Free large drink")
                            .AddTerms("drink", "drink", "free drink");
                        return true;
                    }))
                .Confirm(async (state) =>
                {
                    var cost = 0.0;
                    switch (state.Length)
                    {
                        case LengthOptions.SixInch:
                            cost = 5.0;
                            break;
                        case LengthOptions.FootLong:
                            cost = 6.5;
                            break;
                    }
                    return new PromptAttribute($"Total for your sandwich is ${cost:F2} is that ok?");
                })
                .Field(nameof(DeliveryAddress),
                    validate: async (state, response) =>
                    {
                        var result = new ValidateResult { IsValid = true, Value = response };
                        var address = (response as string).Trim();
                        if (address.Length > 0 && address[0] < '0' || address[0] > '9')
                        {
                            result.Feedback = "Address must start with a number.";
                            result.IsValid = false;
                        }
                        return result;
                    })
                .Field(nameof(DeliveryTime), "What time do you need your sandwich? {||}")
                .Confirm("Do you want to order your {Length} {Sandwich} on {Bread} {&Bread} with {[{Cheese} {Toppings} {Sauces}]} to be sent to {DeliveryAddress} {?at {DeliveryTime:t}}?")
                .AddRemainingFields()
                .Message("Thanks for ordering a sandwich!")
                .Build();
        }

        //public static IForm<JObject> BuildJsonForm()
        //{
        //    var schema = JObject.Parse(System.IO.File.ReadAllText(@"AnnotatedSandwich.json"));
        //    OnCompletionAsyncDelegate<JObject> processOrder = async (context, state) =>
        //    {
        //        await context.PostAsync(DynamicSandwich)
        //    };
        //}
    }
}
