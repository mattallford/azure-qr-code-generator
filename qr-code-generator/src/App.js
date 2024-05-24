import React, { useState } from 'react';
import './App.css';

function App() {
  const [url, setUrl] = useState('');
  const [qrCodeUrl, setQrCodeUrl] = useState('');

  const handleSubmit = async (event) => {
    event.preventDefault();
    const response = await fetch(`https://maqrcg-func-app-001.azurewebsites.net/api/GenerateQRCode?url=${encodeURIComponent(url)}`);
    const data = await response.json();
    setQrCodeUrl(data.qr_code_url);
  };

  return (
    <div className="App">
      <h1>QR Code Generator</h1>
      <form onSubmit={handleSubmit}>
        <input
          type="url"
          placeholder="Enter URL"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          required
        />
        <button type="submit">Generate QR Code</button>
      </form>
      {qrCodeUrl && <div>
        <h2>Generated QR Code:</h2>
        <img src={qrCodeUrl} alt="QR Code" />
      </div>}
    </div>
  );
}

export default App;
