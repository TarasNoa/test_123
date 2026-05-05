use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum WalletType { Hot, Cold, Hardware, MultiSig }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MultiCurrencyWallet {
    pub id: String,
    pub user_id: String,
    pub wallet_type: WalletType,
    pub balances: HashMap<String, f64>,
    pub frozen_balance: HashMap<String, f64>,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WalletAddress {
    pub id: String,
    pub wallet_id: String,
    pub currency: String,
    pub address: String,
    pub is_active: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WalletTransaction {
    pub id: String,
    pub wallet_id: String,
    pub tx_type: String,
    pub currency: String,
    pub amount: f64,
    pub from_address: String,
    pub to_address: String,
    pub status: String,
    pub tx_hash: String,
}

impl MultiCurrencyWallet {
    pub fn new(id: String, user_id: String, wallet_type: WalletType) -> Self {
        MultiCurrencyWallet {
            id,
            user_id,
            wallet_type,
            balances: HashMap::new(),
            frozen_balance: HashMap::new(),
            created_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
        }
    }

    pub fn freeze(&mut self, currency: &str, amount: f64) -> Result<(), String> {
        let balance = self.balances.get(currency).copied().unwrap_or(0.0);
        if balance >= amount {
            *self.frozen_balance.entry(currency.to_string()).or_insert(0.0) += amount;
            *self.balances.get_mut(currency).unwrap() -= amount;
            Ok(())
        } else {
            Err("Insufficient balance".to_string())
        }
    }

    pub fn unfreeze(&mut self, currency: &str, amount: f64) -> Result<(), String> {
        let frozen = self.frozen_balance.get(currency).copied().unwrap_or(0.0);
        if frozen >= amount {
            *self.frozen_balance.get_mut(currency).unwrap() -= amount;
            *self.balances.entry(currency.to_string()).or_insert(0.0) += amount;
            Ok(())
        } else {
            Err("Insufficient frozen balance".to_string())
        }
    }

    pub fn get_available_balance(&self, currency: &str) -> f64 {
        self.balances.get(currency).copied().unwrap_or(0.0)
    }

    pub fn get_total_balance(&self, currency: &str) -> f64 {
        let available = self.balances.get(currency).copied().unwrap_or(0.0);
        let frozen = self.frozen_balance.get(currency).copied().unwrap_or(0.0);
        available + frozen
    }
}
