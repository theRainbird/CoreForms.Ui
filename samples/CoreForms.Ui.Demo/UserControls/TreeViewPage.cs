using CoreForms.Ui.Controls.Advanced;
using CoreForms.Ui.Controls.Basic;
using CoreForms.Ui.Resources;
using CoreForms.Ui.Controls;
using Graphics = CoreForms.Ui.Rendering.Graphics;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Demo.UserControls;

/// <summary>
/// Demonstrates the TreeView control with an ImageList.
/// </summary>
public class TreeViewPage : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreeViewPage"/> class.
    /// </summary>
    public TreeViewPage()
    {
        var imageList = new ImageList();
        imageList.Add(Icons.DocumentFolder24 ?? Icons.Document24!);
        imageList.Add(Icons.DocumentFolder24 ?? Icons.Document24!);
        imageList.Add(Icons.Document24!);

        var treeView = new TreeView
        {
            Location = new Point(10, 10),
            Size = new Size(400, 300),
            ImageList = imageList
        };

        var root1 = new TreeNode(SR.GetString("TreeNodeRoot1")) { ImageIndex = 0 };
        var child1 = new TreeNode(SR.GetString("TreeNodeChild1")) { ImageIndex = 1 };
        var child2 = new TreeNode(SR.GetString("TreeNodeChild2")) { ImageIndex = 2 };
        root1.Add(child1);
        root1.Add(child2);

        var root2 = new TreeNode(SR.GetString("TreeNodeRoot2")) { ImageIndex = 0 };
        var subRoot = new TreeNode(SR.GetString("TreeNodeSubRoot")) { ImageIndex = 1 };
        subRoot.Add(new TreeNode(SR.GetString("TreeNodeLeafA")) { ImageIndex = 2 });
        subRoot.Add(new TreeNode(SR.GetString("TreeNodeLeafB")) { ImageIndex = 2 });
        root2.Add(subRoot);

        treeView.Nodes.Add(root1);
        treeView.Nodes.Add(root2);

        Controls.Add(treeView);
    }
}
