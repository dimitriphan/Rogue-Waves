// SONAR's button: one press sends the command straight away (the game's Entity_Command_Simple), and the icon greys out
// while SONAR recharges, the way the game's on/off tower toggles show "Off" (Entity_Command_Tower_Simple_Toggle reads the
// command's Goalstring, which the server keeps in Entity_Master.Commandlist).
public class ArgoSonarButton : Entity_Command_Simple
{
    static readonly Dictionary<UnityEngine.UI.Image, Color> normal = new Dictionary<UnityEngine.UI.Image, Color>();

    public override void Render(GameObject Button)
    {
        Getnewlink();
        if (Button == null)
            return;
        Transform icon = Button.transform.Find("Icon");
        UnityEngine.UI.Image image = icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null;
        if (image == null)
            return;
        Color look;
        if (!normal.TryGetValue(image, out look))
        {
            if (normal.Count > 32)
                normal.Clear();
            normal[image] = look = image.color;
        }
        image.color = Link.Goalstring.ToString() == "Off" ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : look;
    }
}
