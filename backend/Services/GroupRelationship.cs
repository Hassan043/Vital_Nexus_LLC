namespace NutrientInsight.Api.Services;

public static class GroupRelationships
{
    public static Dictionary<string, GroupEducation> GetGroupEducation()
    {
        return new Dictionary<string, GroupEducation>
        {
            ["Lipid Panel"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about how your body processes fats and cholesterol.",
                FoodPatterns = new[]
                {
                    "Fiber-rich foods like oats, beans, and vegetables",
                    "Unsaturated fats from sources like olive oil, nuts, and fish",
                    "Limiting ultra-processed foods and trans fats"
                },
                VitaminsMinerals = new[]
                {
                    "Omega-3 fatty acids (as a nutrient concept - discuss sources with your clinician)",
                    "Plant sterols (found naturally in some foods)"
                },
                ExerciseHabits = new[]
                {
                    "Regular walking or other aerobic activity",
                    "Strength training a few times per week",
                    "Consistency over intensity"
                }
            },
            ["Glucose Markers"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about how your body manages blood sugar over time.",
                FoodPatterns = new[]
                {
                    "Whole grains and complex carbohydrates",
                    "Pairing carbs with protein or healthy fats",
                    "Eating at regular intervals",
                    "Vegetables, especially non-starchy varieties"
                },
                VitaminsMinerals = new[]
                {
                    "Chromium (discuss with your clinician)",
                    "Magnesium (found in nuts, seeds, leafy greens)"
                },
                ExerciseHabits = new[]
                {
                    "Movement after meals",
                    "Mix of aerobic and resistance activities",
                    "Finding activities you enjoy and can maintain"
                }
            },
            ["Thyroid"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about thyroid function and metabolism.",
                FoodPatterns = new[]
                {
                    "Adequate protein intake",
                    "Iodine-containing foods like fish and dairy (if appropriate)",
                    "Selenium-rich foods like Brazil nuts"
                },
                VitaminsMinerals = new[]
                {
                    "Selenium (discuss sources with your clinician)",
                    "Iodine (discuss with your clinician)",
                    "Zinc (found in meat, shellfish, legumes)"
                },
                ExerciseHabits = new[]
                {
                    "Regular movement that feels good",
                    "Balance between activity and rest",
                    "Managing stress through movement"
                }
            },
            ["Electrolytes"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about fluid balance and mineral status.",
                FoodPatterns = new[]
                {
                    "Adequate hydration throughout the day",
                    "Balanced whole foods",
                    "Potassium-rich foods like bananas, potatoes, leafy greens"
                },
                VitaminsMinerals = new[]
                {
                    "Magnesium (found in nuts, seeds, whole grains)",
                    "Potassium (from food sources)"
                },
                ExerciseHabits = new[]
                {
                    "Staying hydrated during activity",
                    "Balanced activity levels",
                    "Listening to your body"
                }
            },
            ["Liver"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about liver function and overall metabolic health.",
                FoodPatterns = new[]
                {
                    "Whole foods with variety",
                    "Limiting alcohol if appropriate",
                    "Adequate hydration"
                },
                VitaminsMinerals = new[]
                {
                    "B vitamins (discuss with your clinician)",
                    "Antioxidants from colorful vegetables"
                },
                ExerciseHabits = new[]
                {
                    "Regular physical activity",
                    "Maintaining a healthy weight",
                    "Consistency over time"
                }
            },
            ["Blood Counts"] = new GroupEducation
            {
                WhyReviewedTogether = "These markers are often reviewed together to provide context about blood cell health and oxygen transport.",
                FoodPatterns = new[]
                {
                    "Iron-rich foods like red meat, beans, and fortified cereals",
                    "Vitamin C-rich foods to help with iron absorption",
                    "B12 sources like meat, fish, or fortified foods"
                },
                VitaminsMinerals = new[]
                {
                    "Iron (discuss with your clinician before supplementing)",
                    "Folate (found in leafy greens and legumes)",
                    "B12 (especially important for vegetarians/vegans)"
                },
                ExerciseHabits = new[]
                {
                    "Regular aerobic activity",
                    "Building up gradually if starting new exercise",
                    "Rest and recovery"
                }
            }
        };
    }
}

public class GroupEducation
{
    public string WhyReviewedTogether { get; set; } = string.Empty;
    public string[] FoodPatterns { get; set; } = Array.Empty<string>();
    public string[] VitaminsMinerals { get; set; } = Array.Empty<string>();
    public string[] ExerciseHabits { get; set; } = Array.Empty<string>();
}