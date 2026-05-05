use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SmartContract {
    pub id: String,
    pub name: String,
    pub address: String,
    pub bytecode: String,
    pub abi: String,
    pub deployed_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TokenContract {
    pub contract: SmartContract,
    pub total_supply: f64,
    pub decimals: u8,
    pub owner: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ContractDeployment {
    pub id: String,
    pub contract_id: String,
    pub deployer: String,
    pub network: String,
    pub tx_hash: String,
    pub status: String,
}

impl SmartContract {
    pub fn new(name: String, bytecode: String, abi: String) -> Self {
        SmartContract {
            id: uuid::Uuid::new_v4().to_string(),
            name,
            address: String::new(),
            bytecode,
            abi,
            deployed_at: 0,
        }
    }
}
