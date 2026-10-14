using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private SignaturePicker? _signaturePicker;

    /// <summary>
    /// Adds or removes the signature picker tab.
    /// </summary>
    public void RefreshSignaturePicker()
    {
        if (_signaturePicker != null)
            return;

        _signaturePicker = new SignaturePicker();
        TabContainer.AddChild(_signaturePicker);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-signature-tab"));

        _signaturePicker.OnSignatureSelected += signature =>
        {
            // В будущем: сохранять выбор подписи в профиль
            // Сейчас просто логируем, реальный выбор будет подшиваться при подписании
        };
    }

    private void UpdateSignaturePicker()
    {
        // В будущем: загружать сохранённую подпись из профиля
    }
}
