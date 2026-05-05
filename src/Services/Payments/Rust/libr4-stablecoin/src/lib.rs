use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
pub enum StablecoinType { USDC, USDT, DAI, BUSD }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StablecoinBalance {
    pub user_id: String,
    pub stablecoin: StablecoinType,
    pub balance: f64,
    pub locked: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StablecoinTransaction {
    pub id: String,
    pub from: String,
    pub to: String,
    pub stablecoin: StablecoinType,
    pub amount: f64,
    pub tx_hash: String,
    pub status: String,
}

impl StablecoinBalance {
    pub fn available(&self) -> f64 {
        self.balance - self.locked
    }

    pub fn transfer(&mut self, amount: f64) -> Result<(), String> {
        if self.available() >= amount {
            self.balance -= amount;
            Ok(())
        } else {
            Err("Insufficient balance".to_string())
        }
    }
}
