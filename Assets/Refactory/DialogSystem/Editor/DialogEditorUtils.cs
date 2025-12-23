public static class DialogEditorUtils
{
    public static string PotionLabel(PotionDialogEntry entry)
    {
        return $"{entry.potion}OnStatuses";
    }

    public static string StatusCaseLabel(StatusDialogCase c)
    {
        if (c.requiredStatuses == null || c.requiredStatuses.Count == 0)
            return "None";

        return string.Join("-", c.requiredStatuses);
    }
}
