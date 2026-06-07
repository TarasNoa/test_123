use clap::Parser;
use libr4_fast_context::{build_manifest, search_workspace};
use std::process;

#[derive(Parser)]
#[command(name = "libr4-fast-context")]
struct Cli {
    #[command(subcommand)]
    command: Command,
}

#[derive(Parser)]
enum Command {
    Search {
        #[arg(long)]
        workspace: String,
        #[arg(long)]
        query: String,
        #[arg(long, default_value_t = 120)]
        limit: usize,
        #[arg(long, default_value_t = false)]
        include_tests: bool,
        #[arg(long)]
        languages: Option<String>,
    },
    Manifest {
        #[arg(long)]
        workspace: String,
    },
}

fn main() {
    let cli = Cli::parse();
    let result = match cli.command {
        Command::Search {
            workspace,
            query,
            limit,
            include_tests,
            languages,
        } => {
            let langs = languages.map(|l| {
                l.split(',')
                    .map(|s| s.trim().to_string())
                    .filter(|s| !s.is_empty())
                    .collect::<Vec<_>>()
            });
            search_workspace(&workspace, &query, limit, include_tests, langs.as_deref())
                .map(|hits| serde_json::to_string(&hits).unwrap())
        }
        Command::Manifest { workspace } => build_manifest(&workspace)
            .map(|m| serde_json::to_string(&m).unwrap()),
    };

    match result {
        Ok(json) => println!("{json}"),
        Err(err) => {
            eprintln!("{{\"error\":\"{err}\"}}");
            process::exit(1);
        }
    }
}
