use clap::Parser;

fn main() {
    let cli = btd700::cli::Cli::parse();
    if let Err(e) = btd700::cli::run_cli(cli) {
        eprintln!("\x1b[1;31mError:\x1b[0m {e}");
        std::process::exit(1);
    }
}
