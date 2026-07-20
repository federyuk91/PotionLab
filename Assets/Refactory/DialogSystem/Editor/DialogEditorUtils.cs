using CharacterSystem;

public static class DialogEditorUtils
{
    public static string PotionLabel(PotionDialogEntry entry)
    {
        return $"{entry.potion}OnStatuses";
    }

    public static string StatusCaseLabel(StatusDialogCase c)
    {
        if (c.requiredStatuses == null || c.requiredStatuses.Count == 0)
            return "No Status (empty list)";

        if (c.requiredStatuses.Contains(Status.None))
            return "Status.None (invalid)";

        return string.Join("-", c.requiredStatuses);
    }
}
