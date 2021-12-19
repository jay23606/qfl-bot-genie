# QFL Bot Genie


Updates 3Commas QFL multipair bot settings (MaxActiveDeals, Take Profit, QFL Percentage) based on BTC's 1d RSI and 1h ATRp every few minutes

MaxActiveDeals =  RSI_SCALAR*(70 - RSI_VALUE)

(RSI_SCALAR defaulted to 1.0--this lets you increase or decrease deals relative to RSI value)

QFL% = ATRp * QFLP_SCALAR

(QFLP_SCALAR defaulted to 3.5 but you can increase if you want to be more picky when opening deals relative to ATR)

TP% = ATRp * TP_SCALAR

(TP_SCALAR defaulted to 1.5 but you can increase if you want higher take profit relative to ATR at time deal opens)

Also cycles through the QFL timeframes (original=1h, daytrade=2h, conservative=3h, position=4h) each time so that more QFL deals may be presented

Instructions:

Create new console c# program and include your 3c api key, secret, botId, and accountId (or exchange name) for updating bot settings


You will need to include XCommas.Net and Stock.Indicators nuget packages


Disclaimer: No idea how well this strategy will work over simply running static QFL
