#include "gui.h"
#include "main_window.h"

#include <QApplication>
#include <QSystemTrayIcon>
#include <cstring>

extern "C" int run_qt_gui_app(int argc, const char* const* argv) {
    // Ensure Qt doesn't take ownership of non-mutable pointers
    int c = argc;
    char** v = const_cast<char**>(argv);

    QApplication app(c, v);
    app.setApplicationName("Sennheiser Dongle Control");
    app.setOrganizationName("Sennheiser");
    app.setQuitOnLastWindowClosed(false);

    bool startMinimized = false;
    for (int i = 1; i < argc; ++i) {
        if (argv[i] && (strcmp(argv[i], "--tray") == 0 || strcmp(argv[i], "--minimized") == 0 ||
                        strcmp(argv[i], "-t") == 0 || strcmp(argv[i], "-m") == 0)) {
            startMinimized = true;
        }
    }

    MainWindow window;
    bool hasTray = QSystemTrayIcon::isSystemTrayAvailable();
    if (!startMinimized || !hasTray) {
        window.show();
    }

    return app.exec();
}

