use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq, Hash)]
pub enum Currency {
    USD, EUR, GBP, JPY, CAD, AUD, CHF, CNY, INR, BRL, RUB, ZAR,
    BTC, ETH, USDC, USDT,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ExchangeRate {
    pub from: Currency,
    pub to: Currency,
    pub rate: f64,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Wallet {
    pub id: String,
    pub user_id: String,
    pub balances: HashMap<Currency, f64>,
    pub created_at: u64,
}

impl Wallet {
    pub fn new(id: String, user_id: String) -> Self {
        Wallet {
            id,
            user_id,
            balances: HashMap::new(),
            created_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
        }
    }

    pub fn deposit(&mut self, currency: Currency, amount: f64) {
        *self.balances.entry(currency).or_insert(0.0) += amount;
    }

    pub fn withdraw(&mut self, currency: Currency, amount: f64) -> Result<(), String> {
        let balance = self.balances.entry(currency).or_insert(0.0);
        if *balance >= amount {
            *balance -= amount;
            Ok(())
        } else {
            Err("Insufficient funds".to_string())
        }
    }

    pub fn get_balance(&self, currency: Currency) -> f64 {
        self.balances.get(&currency).copied().unwrap_or(0.0)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ConversionResult {
    pub from_amount: f64,
    pub from_currency: Currency,
    pub to_amount: f64,
    pub to_currency: Currency,
    pub rate: f64,
}

pub fn convert_currency(
    from_amount: f64,
    from_currency: Currency,
    to_currency: Currency,
    rate: f64,
) -> ConversionResult {
    let to_amount = from_amount * rate;
    ConversionResult {
        from_amount,
        from_currency,
        to_amount,
        to_currency,
        rate,
    }
}
