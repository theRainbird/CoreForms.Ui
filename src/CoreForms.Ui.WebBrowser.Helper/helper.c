#include <gtk/gtk.h>
#include <webkit2/webkit2.h>
#include <stdlib.h>

int main(int argc, char *argv[])
{
    gtk_init(&argc, &argv);

    const char *url = argc > 1 ? argv[1] : "about:blank";

    GtkWidget *window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
    gtk_window_set_default_size(GTK_WINDOW(window), 1024, 768);
    gtk_window_set_title(GTK_WINDOW(window), "Web Browser");

    GtkWidget *webview = webkit_web_view_new();
    gtk_container_add(GTK_CONTAINER(window), webview);
    webkit_web_view_load_uri(WEBKIT_WEB_VIEW(webview), url);

    gtk_widget_show_all(window);
    gtk_main();

    return 0;
}
