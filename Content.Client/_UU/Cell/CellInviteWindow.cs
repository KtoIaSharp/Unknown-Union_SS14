using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._UU.Cell;

public sealed class CellInviteWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    public readonly Button DeclineButton;
    public readonly Label Prompt;

    public CellInviteWindow(string cellName)
    {
        Title = Loc.GetString("cell-invite-title");

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                (Prompt = new Label
                {
                    Text = Loc.GetString("cell-invite-prompt", ("name", (object)cellName)),
                    Margin = new Thickness(10, 10, 10, 15),
                }),
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Align = AlignMode.Center,
                    HorizontalAlignment = HAlignment.Center,
                    SeparationOverride = 20,
                    Children =
                    {
                        (AcceptButton = new Button
                        {
                            Text = Loc.GetString("cell-invite-accept"),
                            MinSize = new(100, 0),
                        }),
                        (DeclineButton = new Button
                        {
                            Text = Loc.GetString("cell-invite-decline"),
                            MinSize = new(100, 0),
                        }),
                    }
                },
            }
        });
    }
}
