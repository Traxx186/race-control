using RaceControl.Category;
using RaceControl.Track;

namespace RaceControl;

public class CategoryService
{
    private ICategory _activeCategory;

    public event Action<FlagData>? OnCategoryFlagChange;

    public CategoryService()
    {
        _activeCategory = new Formula1("https://livetiming.formula1.com");
        _activeCategory.OnFlagParsed += data => OnCategoryFlagChange?.Invoke(data);
    }

    public void Start() => _activeCategory.Start();
}