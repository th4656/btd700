#include "gui.h"
#include "main_window.h"

#include <QApplication>

extern "C" int run_qt_gui_app(int argc, const char* const* argv) {
    // Ensure Qt doesn't take ownership of non-mutable pointers
    int c = argc;
    char** v = const_cast<char**>(argv);

    QApplication app(c, v);
    app.setApplicationName("Sennheiser Dongle Control");
    app.setOrganizationName("Sennheiser");

    MainWindow window;
    window.show();

    return app.exec();
}
