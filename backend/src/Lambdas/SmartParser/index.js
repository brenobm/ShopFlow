const https = require('https');

exports.handler = async (event) => {
    const text = event.arguments.text;
    const apiKey = process.env.GEMINI_API_KEY;

    if (!apiKey) {
        throw new Error("Gemini API Key is not configured in Lambda environment variables.");
    }

    const data = JSON.stringify({
        contents: [{
            parts: [{
                text: `Extract standardized data from this shopping list item: '${text}'. Return ONLY raw JSON with keys: 'Name' (string, capitalized), 'Quantity' (number), 'Unit' (string, short form like kg, l, un), 'Category' (one of: Produce, Dairy & Eggs, Meat & Poultry, Bakery, Frozen Foods, Pantry, Beverages, Household, Personal Care, Other), 'Emoji' (single char). Example input: 'milk 2l', Output: { "Name": "Milk", "Quantity": 2, "Unit": "l", "Category": "Dairy & Eggs", "Emoji": "🥛" }.`
            }]
        }],
        generationConfig: {
            response_mime_type: "application/json"
        }
    });

    const options = {
        hostname: 'generativelanguage.googleapis.com',
        path: `/v1beta/models/gemini-2.5-flash:generateContent?key=${apiKey}`,
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': Buffer.byteLength(data)
        }
    };

    return new Promise((resolve, reject) => {
        let settled = false;
        const done = (fn, val) => { if (!settled) { settled = true; fn(val); } };

        const timer = setTimeout(() => {
            req.destroy();
            done(reject, new Error('Gemini API request timed out after 25s'));
        }, 25000);

        const req = https.request(options, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                clearTimeout(timer);
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        const jsonResponse = JSON.parse(body);

                        // Extract JSON from Gemini Response
                        const candidates = jsonResponse.candidates;
                        if (candidates && candidates.length > 0) {
                            const parts = candidates[0].content.parts;
                            if (parts && parts.length > 0) {
                                const textResult = parts[0].text;
                                // Return the parsed object directly to AppSync
                                done(resolve, JSON.parse(textResult));
                            } else {
                                done(reject, new Error("No parts in Gemini response"));
                            }
                        } else {
                            done(reject, new Error("No candidates in Gemini response"));
                        }
                    } catch (e) {
                        done(reject, new Error(`Failed to parse response: ${e.message}`));
                    }
                } else {
                    done(reject, new Error(`Gemini API Error: ${res.statusCode} ${body}`));
                }
            });
        });

        req.on('error', (e) => {
            clearTimeout(timer);
            done(reject, new Error(`Request Error: ${e.message}`));
        });

        req.write(data);
        req.end();
    });
};
