using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Controls.Advanced;

namespace OldSchoolForms.Ui;

/// <summary>
/// Displays a message box dialog that can contain text, buttons, and symbols
/// that inform and instruct the user. This class cannot be inherited.
/// </summary>
public static class MessageBox
{
    /// <summary>
    /// Displays a message box with the specified text.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(string text)
    {
        return Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
    }

    /// <summary>
    /// Displays a message box with the specified text and caption.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(string text, string caption)
    {
        return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
    }

    /// <summary>
    /// Displays a message box with the specified text, caption, and buttons.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <param name="buttons">One of the MessageBoxButtons values that specifies which buttons to display.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
    {
        return Show(text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
    }

    /// <summary>
    /// Displays a message box with the specified text, caption, buttons, and icon.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <param name="buttons">One of the MessageBoxButtons values that specifies which buttons to display.</param>
    /// <param name="icon">One of the MessageBoxIcon values that specifies which icon to display.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        return Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
    }

    /// <summary>
    /// Displays a message box with the specified text, caption, buttons, icon, and default button.
    /// </summary>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <param name="buttons">One of the MessageBoxButtons values that specifies which buttons to display.</param>
    /// <param name="icon">One of the MessageBoxIcon values that specifies which icon to display.</param>
    /// <param name="defaultButton">One of the MessageBoxDefaultButton values that specifies the default button.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        Form? owner = Platform.Platform.FocusedWindow;
        return ShowCore(owner, text, caption, buttons, icon, defaultButton);
    }

    /// <summary>
    /// Displays a message box with the specified owner, text, caption, buttons, icon, and default button.
    /// </summary>
    /// <param name="owner">The owner form, which will be disabled while the message box is shown.</param>
    /// <param name="text">The text to display in the message box.</param>
    /// <param name="caption">The text to display in the title bar of the message box.</param>
    /// <param name="buttons">One of the MessageBoxButtons values that specifies which buttons to display.</param>
    /// <param name="icon">One of the MessageBoxIcon values that specifies which icon to display.</param>
    /// <param name="defaultButton">One of the MessageBoxDefaultButton values that specifies the default button.</param>
    /// <returns>One of the DialogResult values.</returns>
    public static DialogResult Show(Control owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        Form? ownerForm = owner?.FindForm();
        return ShowCore(ownerForm, text, caption, buttons, icon, defaultButton);
    }

    private static DialogResult ShowCore(Form? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        if (owner == null)
        {
            throw new InvalidOperationException("MessageBox requires an owner form.");
        }

        var overlay = new MessageBoxOverlay(owner, text, caption, buttons, icon, defaultButton);
        owner.Controls.Add(overlay);
        owner.PerformLayout();
        overlay.Initialize();

        try
        {
            while (overlay.DialogResult == DialogResult.None)
            {
                Platform.Platform.ProcessEvents(Application.Instance);
            }

            return overlay.DialogResult;
        }
        finally
        {
            owner.Controls.Remove(overlay);
            owner.PerformLayout();
        }
    }
}