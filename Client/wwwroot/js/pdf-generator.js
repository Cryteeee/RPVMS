window.jsPDF = window.jspdf.jsPDF;

function generatePdf(data) {
    // Create new jsPDF instance in portrait, A4 format
    const doc = new jsPDF({
        orientation: 'portrait',
        unit: 'mm',
        format: 'a4'
    });
    
    // Set initial y position
    let y = 20;
    
    // Add letterhead
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text('Roosevelt Park Village', 105, y, { align: 'center' });
    
    y += 8;
    doc.setFontSize(10);
    doc.setFont('helvetica', 'normal');
    doc.text('Subic Bay Freeport Zone', 105, y, { align: 'center' });
    
    y += 6;
    doc.text('Tel: +63 (047) 252-XXXX | Email: info@rpv.com', 105, y, { align: 'center' });
    
    // Add horizontal line
    y += 8;
    doc.setLineWidth(0.5);
    doc.line(20, y, 190, y);
    
    // Add date and sender info section
    y += 15;
    doc.text(data.generatedDate, 20, y);
    
    // Add sender's email in a prominent position
    y += 8;
    doc.setFont('helvetica', 'bold');
    doc.text('From:', 20, y);
    doc.setFont('helvetica', 'normal');
    doc.text(data.userEmail || 'N/A', 35, y);
    
    // Add reference number
    y += 8;
    doc.text(`Reference No: RPV-${data.id || 'XXXX'}-${new Date().getFullYear()}`, 20, y);
    
    // Add subject line
    y += 10;
    doc.setFont('helvetica', 'bold');
    doc.text('Subject:', 20, y);
    doc.setFont('helvetica', 'normal');
    doc.text(data.title, 35, y);
    
    // Add status
    y += 8;
    doc.setFont('helvetica', 'bold');
    doc.text('Status:', 20, y);
    doc.setFont('helvetica', 'normal');
    doc.text(data.status, 33, y);
    
    // Add priority level
    y += 8;
    doc.setFont('helvetica', 'bold');
    doc.text('Priority Level:', 20, y);
    doc.setFont('helvetica', 'normal');
    doc.text(data.priorityLevel || data.urgencyLevel || 'N/A', 45, y);
    
    // Add description section
    y += 12;
    doc.setFont('helvetica', 'bold');
    doc.text('Description:', 20, y);
    
    // Handle description text wrapping
    y += 8;
    doc.setFont('helvetica', 'normal');
    const splitDescription = doc.splitTextToSize(data.description, 150);
    doc.text(splitDescription, 20, y);
    
    // Add footer
    doc.setFontSize(8);
    doc.text('This document is computer-generated and does not require a signature.', 105, 285, { align: 'center' });
    doc.text(`Generated on: ${data.generatedDate}`, 105, 290, { align: 'center' });

    // Create the PDF blob
    const pdfBlob = doc.output('blob');
    const blobUrl = URL.createObjectURL(pdfBlob);

    // Remove any existing PDF buttons container
    const existingContainer = document.getElementById('pdf-buttons-container');
    if (existingContainer) {
        existingContainer.remove();
    }

    // Create buttons container
    const buttonsContainer = document.createElement('div');
    buttonsContainer.id = 'pdf-buttons-container';
    Object.assign(buttonsContainer.style, {
        position: 'fixed',
        top: '20px',
        right: '20px',
        zIndex: '9999',
        display: 'flex',
        flexDirection: 'column',
        gap: '10px',
        background: 'white',
        padding: '15px',
        borderRadius: '8px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
        opacity: '1',
        transition: 'opacity 0.3s ease-out'
    });

    // Create close button
    const closeButton = document.createElement('button');
    closeButton.innerHTML = '<i class="fas fa-times"></i>';
    Object.assign(closeButton.style, {
        position: 'absolute',
        top: '5px',
        right: '5px',
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        fontSize: '14px',
        color: '#666'
    });
    closeButton.onclick = () => {
        removeContainer();
    };

    // Create preview button
    const previewButton = document.createElement('button');
    previewButton.innerHTML = '<i class="fas fa-eye"></i> Preview PDF';
    previewButton.onclick = () => window.open(blobUrl, '_blank');
    styleButton(previewButton, '#1a237e');

    // Create download button
    const downloadButton = document.createElement('button');
    downloadButton.innerHTML = '<i class="fas fa-download"></i> Download PDF';
    downloadButton.onclick = () => {
        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = `${data.title || 'concern'}-details.pdf`;
        link.click();
    };
    styleButton(downloadButton, '#2e7d32');

    // Add buttons to container
    buttonsContainer.appendChild(closeButton);
    buttonsContainer.appendChild(previewButton);
    buttonsContainer.appendChild(downloadButton);

    // Add container to document
    document.body.appendChild(buttonsContainer);

    // Function to remove container with fade effect
    function removeContainer() {
        buttonsContainer.style.opacity = '0';
        setTimeout(() => {
            buttonsContainer.remove();
            URL.revokeObjectURL(blobUrl);
        }, 300);
    }

    // Set timer to remove container after 2 seconds
    setTimeout(() => {
        removeContainer();
    }, 2000);
}

function styleButton(button, color) {
    Object.assign(button.style, {
        padding: '10px 20px',
        backgroundColor: color,
        color: 'white',
        border: 'none',
        borderRadius: '5px',
        cursor: 'pointer',
        fontWeight: 'bold',
        boxShadow: '0 2px 4px rgba(0,0,0,0.2)',
        transition: 'transform 0.2s, box-shadow 0.2s',
        width: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '8px'
    });
    
    button.onmouseover = () => {
        button.style.transform = 'translateY(-2px)';
        button.style.boxShadow = '0 4px 8px rgba(0,0,0,0.2)';
    };
    
    button.onmouseout = () => {
        button.style.transform = 'translateY(0)';
        button.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
    };
}

function addField(doc, label, value, y) {
    doc.setFont('helvetica', 'bold');
    doc.text(`${label}:`, 20, y);
    doc.setFont('helvetica', 'normal');
    doc.text(value || '', 80, y);
} 